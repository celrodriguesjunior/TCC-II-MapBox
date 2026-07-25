using System.Globalization;
using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tcc.Collector;
using Tcc.Domain;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var args_ = Environment.GetCommandLineArgs()[1..];

if (args_.Contains("--help") || args_.Contains("-h"))
{
    PrintHelp();
    return 0;
}

var builder = Host.CreateApplicationBuilder(args_);

// Carregado a partir do diretório do binário, não do diretório de trabalho: o host resolve
// appsettings.json contra a raiz de conteúdo (que é o cwd), e rodar de outra pasta faria a
// configuração ser silenciosamente ignorada em favor dos padrões compilados.
builder.Configuration.AddJsonFile(
    Path.Combine(AppContext.BaseDirectory, "appsettings.json"), optional: false, reloadOnChange: false);

builder.Configuration.AddUserSecrets(typeof(CollectorOptions).Assembly, optional: true);
builder.Configuration.AddEnvironmentVariables("Tcc__");

builder.Services
    .AddOptions<CollectorOptions>()
    .Bind(builder.Configuration.GetSection(CollectorOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();   // falhar no boot, não na primeira requisição

var dryRun = args_.Contains("--dry-run");

// ------------------------------------------------------------------ catálogo

var options = new CollectorOptions();
builder.Configuration.GetSection(CollectorOptions.SectionName).Bind(options);

if (!File.Exists(options.SegmentsFile))
{
    Console.Error.WriteLine($"""
        ERRO: catálogo de segmentos não encontrado em {options.SegmentsFile}

        Ele é produzido pela sondagem de cobertura:
          dotnet run --project src/Tcc.Probe -- --confirm
        """);
    return 1;
}

SegmentCatalog catalog;
try
{
    catalog = JsonSerializer.Deserialize<SegmentCatalog>(
        await File.ReadAllTextAsync(options.SegmentsFile),
        new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower })
        ?? throw new InvalidOperationException("catálogo vazio");
}
catch (Exception ex) when (ex is JsonException or InvalidOperationException)
{
    Console.Error.WriteLine($"ERRO ao ler {options.SegmentsFile}: {ex.Message}");
    return 1;
}

if (catalog.Segments.Count == 0)
{
    Console.Error.WriteLine("ERRO: o catálogo não contém nenhum segmento.");
    return 1;
}

// ------------------------------------------------------------------ projeção

var projected = catalog.Segments
    .GroupBy(s => s.Tier)
    .Sum(g => (int)(g.Count() * (1440.0 / options.CadenceFor(g.Key))));

Console.WriteLine($"""

    Coletor — Rio Branco, Brusque/SC
      catálogo          : {options.SegmentsFile}
      segmentos         : {catalog.Segments.Count} em {catalog.Segments.Select(s => s.StreetName).Distinct(StringComparer.Ordinal).Count()} ruas
      saída             : {Path.GetFullPath(options.OutputDirectory)}
    """);

Console.WriteLine("\n  Volume diário projetado (tráfego)");
foreach (var tier in catalog.Segments.GroupBy(s => s.Tier).OrderBy(g => g.Key))
{
    var cadence = options.CadenceFor(tier.Key);
    var calls = (int)(tier.Count() * (1440.0 / cadence));
    Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
        $"    {tier.Key,-12} {tier.Count(),3} segmentos a cada {cadence,3} min = {calls,5} chamadas/dia"));
}

var weatherCalls = 1440 / options.WeatherCadenceMinutes;
Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
    $"    {"TOTAL",-12} {projected,5} chamadas/dia de tráfego (teto {options.DailyRequestBudget})"));
Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
    $"    {"clima",-12} {weatherCalls,5} chamadas/dia (Open-Meteo, fora do orçamento TomTom)"));

if (projected > options.DailyRequestBudget)
{
    Console.Error.WriteLine($"""

        ERRO: a projeção de {projected} chamadas/dia excede o teto de {options.DailyRequestBudget}.

        Aumente a cadência das camadas em appsettings.json, ou reduza o número de segmentos
        no catálogo. O coletor não inicia em configuração que estoura a cota.
        """);
    return 1;
}

Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
    $"\n  folga: {options.DailyRequestBudget - projected} chamadas/dia " +
    $"({(options.DailyRequestBudget - projected) / (double)options.DailyRequestBudget:P0})"));

if (dryRun)
{
    Console.WriteLine("\nDRY RUN — nada foi coletado. Remova --dry-run para iniciar.\n");
    return 0;
}

if (string.IsNullOrWhiteSpace(options.TomTom.ApiKey))
{
    Console.Error.WriteLine("""

        ERRO: chave da TomTom não configurada.

        Defina de uma destas formas (nenhuma grava a chave no repositório):
          $env:Tcc__Collector__TomTom__ApiKey = "sua-chave"
          dotnet user-secrets set "Collector:TomTom:ApiKey" "sua-chave" --project src/Tcc.Collector
        """);
    return 1;
}

// ------------------------------------------------------------------ serviços

builder.Services.AddSingleton(catalog);
builder.Services.AddSingleton<IRequestBudget, FileRequestBudget>();

builder.Services.AddSingleton<RateLimiter>(_ => new TokenBucketRateLimiter(
    new TokenBucketRateLimiterOptions
    {
        TokenLimit = Math.Max(1, (int)options.RequestsPerSecond),
        TokensPerPeriod = Math.Max(1, (int)options.RequestsPerSecond),
        ReplenishmentPeriod = TimeSpan.FromSeconds(1),
        QueueLimit = int.MaxValue,
        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
        AutoReplenishment = true,
    }));

builder.Services.AddTransient<BudgetHandler>();

// Tráfego: passa pelo handler de orçamento e pela resiliência padrão do .NET (retry com backoff
// e jitter, circuit breaker, timeouts). O handler fica na cadeia para que **as retentativas
// também debitem orçamento** — contar só o sucesso subestimaria o consumo justamente quando a
// API está instável.
builder.Services.AddHttpClient<ITrafficProvider, TomTomTrafficProvider>(c =>
    {
        c.Timeout = TimeSpan.FromSeconds(30);
    })
    .AddHttpMessageHandler<BudgetHandler>()
    .AddStandardResilienceHandler();

// Clima: gratuito e sem chave, então fora do orçamento da TomTom.
builder.Services.AddHttpClient<IWeatherProvider, OpenMeteoWeatherProvider>(c =>
    {
        c.Timeout = TimeSpan.FromSeconds(30);
    })
    .AddStandardResilienceHandler();

builder.Services.AddHostedService<TrafficPollingService>();
builder.Services.AddHostedService<WeatherPollingService>();

builder.Logging.AddSimpleConsole(o =>
{
    o.SingleLine = true;
    o.TimestampFormat = "HH:mm:ss ";
    o.UseUtcTimestamp = true;
});

var host = builder.Build();

Console.WriteLine("\nColetando. Ctrl+C encerra de forma limpa.\n");
await host.RunAsync();

return 0;

static void PrintHelp() => Console.WriteLine("""
    Tcc.Collector — coleta contínua de fluxo de tráfego e clima

    USO
      dotnet run --project src/Tcc.Collector [--dry-run]

    Sem --dry-run, inicia a coleta e roda até Ctrl+C.

    OPÇÕES
      --dry-run     Projeta o volume diário de chamadas e encerra, sem coletar
      -h, --help    Esta ajuda

    CONFIGURAÇÃO
      appsettings.json, seção "Collector". Cadência por camada, teto de orçamento,
      diretório de saída e caminho do catálogo de segmentos.

    CHAVE DA API
      Nunca em arquivo do repositório:
        $env:Tcc__Collector__TomTom__ApiKey = "sua-chave"
        dotnet user-secrets set "Collector:TomTom:ApiKey" "..." --project src/Tcc.Collector

      O clima usa Open-Meteo, que não exige chave para uso não comercial.

    SAÍDA
      data/raw/traffic/AAAA-MM-DD.ndjson    uma linha por leitura de segmento
      data/raw/weather/AAAA-MM-DD.ndjson    uma linha por leitura de clima
      data/raw/budget.json                  contador diário, sobrevive a reinício

    Toda amostra agendada gera um registro, inclusive quando falha — uma lacuna na
    série é sempre explicável.
    """);
