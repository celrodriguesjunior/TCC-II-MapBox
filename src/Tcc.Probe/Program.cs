using System.Globalization;
using Microsoft.Extensions.Configuration;
using Tcc.Probe;

Console.OutputEncoding = System.Text.Encoding.UTF8;

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
    Console.WriteLine("\nInterrompido. Nenhuma chamada adicional será feita.");
};

var args_ = Environment.GetCommandLineArgs()[1..];

if (args_.Contains("--help") || args_.Contains("-h"))
{
    PrintHelp();
    return 0;
}

var options = ParseOptions(args_);
var apiKey = ResolveApiKey(args_);

// A chave nunca é lida de arquivo do repositório nem gravada em disco.
if (options.Confirmed && string.IsNullOrWhiteSpace(apiKey))
{
    Console.Error.WriteLine("""
        ERRO: chave da TomTom não encontrada.

        Defina de uma destas formas (nenhuma grava a chave no repositório):
          setx Tcc__TomTom__ApiKey "sua-chave"        (Windows, persistente)
          $env:Tcc__TomTom__ApiKey = "sua-chave"      (PowerShell, sessão atual)
          dotnet user-secrets set "Tcc:TomTom:ApiKey" "sua-chave" --project src/Tcc.Probe
        """);
    return 1;
}

Directory.CreateDirectory(options.OutputDirectory);

using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };

// ---------------------------------------------------------------- credencial

// Verificada antes de qualquer trabalho caro: uma chamada confirma que a chave vale.
// Sem isto, uma chave inválida gera um relatório inteiro de falsos "sem cobertura" — e faz
// esperar a consulta ao Overpass para nada.
TomTomClient? tomtom = null;

if (options.Confirmed)
{
    tomtom = new TomTomClient(http, apiKey!);

    Console.Write("Verificando credencial da TomTom... ");
    var canary = await tomtom.GetFlowAsync(
        new GeoPoint((options.Bbox.South + options.Bbox.North) / 2, (options.Bbox.West + options.Bbox.East) / 2),
        cts.Token);

    if (canary.IsFatal)
    {
        Console.WriteLine($"FALHOU (HTTP {canary.HttpStatus})");
        Console.Error.WriteLine(canary.HttpStatus switch
        {
            401 or 403 => """

                A TomTom rejeitou a credencial.

                Verifique em https://developer.tomtom.com/user/me/apps :
                  - a chave foi copiada por inteiro (32 caracteres);
                  - o produto "Traffic API" está habilitado para ela;
                  - a chave já propagou — chaves novas podem levar alguns minutos.

                Nenhuma chamada adicional foi feita.
                """,
            429 => "\nCota diária ou taxa excedida. Nenhuma chamada adicional foi feita.",
            _ => "\nFalha de autorização. Nenhuma chamada adicional foi feita.",
        });
        return 1;
    }

    if (canary.HttpStatus is not (200 or 404))
    {
        Console.WriteLine($"resposta inesperada (HTTP {canary.HttpStatus})");
        Console.Error.WriteLine($"Abortando: {canary.Error}");
        return 1;
    }

    Console.WriteLine("ok");
}

// ---------------------------------------------------------------- malha viária

Console.WriteLine($"Consultando OpenStreetMap no recorte {options.Bbox.ToOverpass()} ...");

var overpass = new OverpassClient(http);
IReadOnlyList<OsmWay> ways;
try
{
    ways = await overpass.FetchNamedWaysAsync(options.Bbox, options.HighwayClasses, cts.Token);
}
catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
{
    Console.Error.WriteLine($"ERRO ao consultar o Overpass: {ex.Message}");
    return 1;
}

var streetCount = ways.Select(w => w.Name).Distinct(StringComparer.Ordinal).Count();
Console.WriteLine($"  {ways.Count} trechos, {streetCount} ruas nomeadas");

foreach (var group in ways.GroupBy(w => w.Highway).OrderByDescending(g => g.Count()))
{
    Console.WriteLine($"    {group.Key,-16} {group.Count()}");
}

// ---------------------------------------------------------------- candidatos

var candidates = Evaluator.BuildCandidates(ways, options);

if (candidates.Count > options.MaxCalls)
{
    Console.WriteLine($"\n  {candidates.Count} candidatos excedem o teto de {options.MaxCalls}; truncando.");
    candidates = candidates.Take(options.MaxCalls).ToList();
}

Console.WriteLine($"\nPlano de sondagem");
Console.WriteLine($"  pontos a consultar : {candidates.Count}");
Console.WriteLine($"  ruas cobertas      : {candidates.Select(c => c.StreetName).Distinct(StringComparer.Ordinal).Count()}");
Console.WriteLine($"  chamadas à TomTom  : {candidates.Count} ({candidates.Count / 2400.0:P1} do orçamento diário)");
Console.WriteLine($"  taxa               : {options.RequestsPerSecond:F0} req/s");
Console.WriteLine($"  duração estimada   : ~{candidates.Count / options.RequestsPerSecond:F0} s");

// Sempre grava a seleção de candidatos: é revisável sem gastar uma única chamada.
var candidatesPath = Path.Combine(options.OutputDirectory, "candidates-osm.geojson");
await Reporter.WriteCandidatesGeoJsonAsync(candidatesPath, candidates, CancellationToken.None);
Console.WriteLine($"\n  seleção gravada em {candidatesPath}");

if (!options.Confirmed)
{
    Console.WriteLine("""

        DRY RUN — nenhuma chamada à TomTom foi feita.
        Abra o candidates-osm.geojson num mapa para revisar a seleção.
        Reexecute com --confirm para sondar a cobertura.
        """);
    return 0;
}

// ---------------------------------------------------------------- sondagem

Console.WriteLine("\nSondando...\n");
var results = new List<ProbeResult>(candidates.Count);
var interval = TimeSpan.FromSeconds(1.0 / options.RequestsPerSecond);
var stopwatch = System.Diagnostics.Stopwatch.StartNew();

for (var i = 0; i < candidates.Count; i++)
{
    if (cts.Token.IsCancellationRequested)
    {
        break;
    }

    var candidate = candidates[i];
    var target = TimeSpan.FromTicks(interval.Ticks * i);
    var wait = target - stopwatch.Elapsed;
    if (wait > TimeSpan.Zero)
    {
        try
        {
            await Task.Delay(wait, cts.Token);
        }
        catch (TaskCanceledException)
        {
            break;
        }
    }

    var response = await tomtom!.GetFlowAsync(candidate.QueryPoint, cts.Token);

    // Credencial revogada ou cota estourada no meio da execução: parar imediatamente.
    if (response.IsFatal)
    {
        Console.Error.WriteLine(string.Create(CultureInfo.InvariantCulture,
            $"\nABORTADO no ponto {i + 1}/{candidates.Count}: HTTP {response.HttpStatus}."));
        Console.Error.WriteLine("Os resultados parciais obtidos até aqui foram gravados.");
        break;
    }

    var result = Evaluator.Evaluate(candidate, response, options);
    results.Add(result);

    var mark = result.Verdict switch
    {
        Verdict.Aceito => "ok  ",
        Verdict.GeometriaDivergente => "DESV",
        Verdict.ConfiancaBaixa => "conf",
        Verdict.SemCobertura => "----",
        _ => "ERRO",
    };

    Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
        $"  [{i + 1,3}/{candidates.Count}] {mark}  {candidate.StreetName,-38} {result.Verdict}"));
}

// ---------------------------------------------------------------- artefatos

var geoJsonPath = Path.Combine(options.OutputDirectory, "candidates.geojson");
var ndjsonPath = Path.Combine(options.OutputDirectory, "probe-raw.ndjson");
var reportPath = Path.Combine(options.OutputDirectory, "probe-report.md");

await Reporter.WriteGeoJsonAsync(geoJsonPath, results, CancellationToken.None);
await Reporter.WriteNdjsonAsync(ndjsonPath, results, CancellationToken.None);
await Reporter.WriteMarkdownAsync(reportPath, results, options, CancellationToken.None);

Console.WriteLine("\nResumo");
foreach (var group in results.GroupBy(r => r.Verdict).OrderByDescending(g => g.Count()))
{
    Console.WriteLine($"  {group.Key,-22} {group.Count(),4}");
}

var accepted = results.Count(r => r.Verdict == Verdict.Aceito);
if (accepted > 0)
{
    var cycles = (int)(2400.0 / accepted);
    Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
        $"\n  {accepted} segmentos aceitos → {cycles} ciclos/dia → um a cada {1440.0 / cycles:F1} min"));
}

Console.WriteLine($"\nArtefatos:\n  {geoJsonPath}\n  {ndjsonPath}\n  {reportPath}");
Console.WriteLine("\nAbra o candidates.geojson num mapa e confira os aceitos contra o que você conhece do bairro.");

return 0;

// ---------------------------------------------------------------- auxiliares

static string? ResolveApiKey(string[] args)
{
    var inline = ValueOf(args, "--key");
    if (!string.IsNullOrWhiteSpace(inline))
    {
        return inline;
    }

    var config = new ConfigurationBuilder()
        .AddUserSecrets(typeof(TomTomClient).Assembly, optional: true)
        .AddEnvironmentVariables()
        .Build();

    return config["Tcc:TomTom:ApiKey"] ?? config["TOMTOM_API_KEY"];
}

static ProbeOptions ParseOptions(string[] args)
{
    var options = new ProbeOptions { Confirmed = args.Contains("--confirm") };

    if (ValueOf(args, "--bbox") is { } bbox)
    {
        var p = bbox.Split(',', StringSplitOptions.TrimEntries);
        if (p.Length == 4)
        {
            options = options with
            {
                Bbox = new BoundingBox(
                    double.Parse(p[0], CultureInfo.InvariantCulture),
                    double.Parse(p[1], CultureInfo.InvariantCulture),
                    double.Parse(p[2], CultureInfo.InvariantCulture),
                    double.Parse(p[3], CultureInfo.InvariantCulture)),
            };
        }
    }

    if (Number(args, "--spacing") is { } spacing) options = options with { SpacingMeters = spacing };
    if (Number(args, "--tolerance") is { } tol) options = options with { ToleranceMeters = tol };
    if (Number(args, "--min-confidence") is { } conf) options = options with { MinConfidence = conf };
    if (Number(args, "--max-per-street") is { } per) options = options with { MaxPointsPerStreet = (int)per };
    if (Number(args, "--max-calls") is { } max) options = options with { MaxCalls = (int)max };
    if (ValueOf(args, "--out") is { } dir) options = options with { OutputDirectory = dir };

    return options;
}

static string? ValueOf(string[] args, string name)
{
    var i = Array.IndexOf(args, name);
    return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
}

static double? Number(string[] args, string name) =>
    ValueOf(args, name) is { } v && double.TryParse(v, CultureInfo.InvariantCulture, out var d) ? d : null;

static void PrintHelp() => Console.WriteLine("""
    Tcc.Probe — sondagem de cobertura da TomTom sobre a malha viária do OSM

    USO
      dotnet run --project src/Tcc.Probe [opções]

    Sem --confirm, apenas imprime o plano e o custo em chamadas. Nada é gasto.

    OPÇÕES
      --confirm                Executa de fato as chamadas à TomTom
      --bbox S,W,N,E           Recorte (padrão: Rio Branco, Brusque/SC)
      --spacing METROS         Espaçamento entre pontos na mesma via (padrão 400)
      --max-per-street N       Teto de pontos por rua (padrão 4)
      --max-calls N            Teto absoluto de chamadas (padrão 300)
      --tolerance METROS       Distância aceitável entre geometria casada e OSM (padrão 25)
      --min-confidence 0..1    Confiança mínima da leitura (padrão 0.50)
      --out DIR                Diretório de saída (padrão data/reference)
      --key CHAVE              Chave da TomTom (prefira variável de ambiente)
      -h, --help               Esta ajuda

    CHAVE DA API
      Procurada nesta ordem: --key, user-secrets (Tcc:TomTom:ApiKey),
      variável de ambiente Tcc__TomTom__ApiKey, variável TOMTOM_API_KEY.

    SAÍDA
      candidates.geojson   pontos consultados + geometria casada, para inspeção visual
      probe-raw.ndjson     um registro por candidato, para auditoria
      probe-report.md      resumo com vereditos e cadência viável
    """);
