using System.Text.Json.Serialization;

namespace Tcc.Domain;

/// <summary>
/// Camada de amostragem de um segmento. Define com que frequência ele é consultado.
///
/// Estratificar existe porque o orçamento é fixo: a espinha do bairro precisa de resolução fina
/// para mostrar o congestionamento se propagando, enquanto uma coletora tolera cadência maior.
/// Tratar tudo igual gastaria o mesmo orçamento com muito menos informação.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<SegmentTier>))]
public enum SegmentTier
{
    /// <summary>Eixos estruturais — a maior frequência.</summary>
    Espinha,

    /// <summary>Vias coletoras — frequência intermediária.</summary>
    Coletora,

    /// <summary>Demais vias com cobertura confirmada.</summary>
    Secundaria,
}

/// <summary>
/// Um ponto viário monitorado, aprovado pela sondagem de cobertura.
///
/// O nome é "segmento" e não "rua" de propósito: o flowSegmentData devolve um trecho por
/// coordenada consultada, e uma rua longa rende vários. Em 2022 os dois conceitos foram
/// tratados como um só, e por isso não dava para distinguir onde numa via o congestionamento
/// começava.
/// </summary>
public sealed record Segment
{
    /// <summary>Identificador estável, herdado do candidato aprovado na sondagem.</summary>
    public required string Id { get; init; }

    public required string StreetName { get; init; }

    /// <summary>Classificação do OSM (secondary, tertiary, residential).</summary>
    public required string Highway { get; init; }

    /// <summary>Coordenada consultada na API de tráfego.</summary>
    public required double Lat { get; init; }

    public required double Lon { get; init; }

    public SegmentTier Tier { get; init; } = SegmentTier.Secundaria;

    /// <summary>Way do OpenStreetMap de onde o ponto foi amostrado, para rastrear a procedência.</summary>
    public long OsmWayId { get; init; }

    /// <summary>Confiança medida na sondagem, como registro do que se sabia ao promover o segmento.</summary>
    public double ProbeConfidence { get; init; }

    [JsonIgnore]
    public GeoPoint Point => new(Lat, Lon);
}

/// <summary>Conteúdo do arquivo de referência dos segmentos monitorados.</summary>
public sealed record SegmentCatalog
{
    public int SchemaVersion { get; init; } = 1;

    /// <summary>Quando a sondagem que originou esta lista foi executada.</summary>
    public DateTimeOffset? ProbedAtUtc { get; init; }

    /// <summary>Ponto único de consulta meteorológica — o bairro inteiro tem o mesmo tempo.</summary>
    public required double WeatherLat { get; init; }

    public required double WeatherLon { get; init; }

    public required IReadOnlyList<Segment> Segments { get; init; }
}
