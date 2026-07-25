using System.ComponentModel.DataAnnotations;
using Tcc.Domain;

namespace Tcc.Collector;

/// <summary>
/// Configuração do coletor. Validada no boot (<c>ValidateOnStart</c>) — uma coleta de semanas não
/// pode descobrir que o diretório de saída estava errado na primeira falha de escrita.
/// </summary>
public sealed class CollectorOptions
{
    public const string SectionName = "Collector";

    /// <summary>Raiz onde o NDJSON é gravado. Caminho absoluto ou relativo ao diretório de trabalho.</summary>
    [Required(AllowEmptyStrings = false)]
    public string OutputDirectory { get; set; } = Path.Combine("data", "raw");

    /// <summary>Catálogo de segmentos aprovados na sondagem.</summary>
    [Required(AllowEmptyStrings = false)]
    public string SegmentsFile { get; set; } = Path.Combine("data", "reference", "segments.json");

    /// <summary>
    /// Teto diário de chamadas à API de tráfego. O plano gratuito da TomTom dá 2.500; a folga
    /// existe para absorver retentativas sem estourar.
    /// </summary>
    [Range(1, 100_000)]
    public int DailyRequestBudget { get; set; } = 2_400;

    /// <summary>Requisições por segundo. A TomTom limita a 5.</summary>
    [Range(0.1, 20)]
    public double RequestsPerSecond { get; set; } = 4;

    /// <summary>Minutos entre ciclos, por camada de segmento.</summary>
    public Dictionary<string, int> CadenceMinutesByTier { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        [nameof(SegmentTier.Espinha)] = 10,
        [nameof(SegmentTier.Coletora)] = 20,
        [nameof(SegmentTier.Secundaria)] = 30,
    };

    /// <summary>Minutos entre leituras de clima. Um ponto cobre o bairro inteiro.</summary>
    [Range(1, 1440)]
    public int WeatherCadenceMinutes { get; set; } = 20;

    public TomTomOptions TomTom { get; set; } = new();

    /// <summary>Se verdadeiro, apenas projeta o volume diário e encerra, sem gastar chamada.</summary>
    public bool DryRun { get; set; }

    /// <summary>Cadência da camada, com queda para a mais conservadora se não configurada.</summary>
    public int CadenceFor(SegmentTier tier) =>
        CadenceMinutesByTier.TryGetValue(tier.ToString(), out var minutes) && minutes > 0
            ? minutes
            : 30;
}

public sealed class TomTomOptions
{
    /// <summary>
    /// Nunca vem de arquivo do repositório: user-secrets em desenvolvimento, variável de
    /// ambiente <c>Tcc__Collector__TomTom__ApiKey</c> em produção.
    /// </summary>
    public string? ApiKey { get; set; }

    public string BaseUrl { get; set; } = "https://api.tomtom.com/traffic/services/4/flowSegmentData/absolute/10/json";
}
