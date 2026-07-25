namespace Tcc.Probe;

/// <summary>Retângulo geográfico de busca.</summary>
public readonly record struct BoundingBox(double South, double West, double North, double East)
{
    /// <summary>Ordem exigida pelo Overpass: (sul, oeste, norte, leste).</summary>
    public string ToOverpass() => string.Create(
        System.Globalization.CultureInfo.InvariantCulture,
        $"{South},{West},{North},{East}");

    public static readonly BoundingBox RioBranco = new(
        South: -27.145,
        West: -48.970,
        North: -27.108,
        East: -48.940);
}

/// <summary>Um trecho de via vindo do OpenStreetMap.</summary>
public sealed record OsmWay(
    long Id,
    string Name,
    string Highway,
    bool Oneway,
    IReadOnlyList<GeoPoint> Geometry)
{
    public double LengthMeters => Geo.Length(Geometry);
}

/// <summary>Ponto candidato a virar segmento monitorado.</summary>
public sealed record Candidate(
    string Id,
    string StreetName,
    string Highway,
    long OsmWayId,
    GeoPoint QueryPoint,
    IReadOnlyList<GeoPoint> OsmGeometry);

/// <summary>Resposta útil do flowSegmentData da TomTom.</summary>
public sealed record FlowSegment(
    string Frc,
    int CurrentSpeedKph,
    int FreeFlowSpeedKph,
    int CurrentTravelTimeSeconds,
    int FreeFlowTravelTimeSeconds,
    double Confidence,
    bool RoadClosure,
    IReadOnlyList<GeoPoint> MatchedGeometry);

/// <summary>Veredito da sondagem para um candidato.</summary>
public enum Verdict
{
    /// <summary>Cobertura válida e geometria compatível — pode virar segmento monitorado.</summary>
    Aceito,

    /// <summary>
    /// A TomTom respondeu, mas sobre outra via. É a falha perigosa: HTTP 200, número plausível,
    /// dado errado.
    /// </summary>
    GeometriaDivergente,

    /// <summary>Geometria compatível, mas leitura pouco confiável (provavelmente modelada).</summary>
    ConfiancaBaixa,

    /// <summary>A API não retornou dado utilizável.</summary>
    SemCobertura,

    /// <summary>Falha de transporte, cota ou parsing.</summary>
    Erro,
}

/// <summary>Resultado completo da sondagem de um candidato.</summary>
public sealed record ProbeResult(
    Candidate Candidate,
    Verdict Verdict,
    string Reason,
    FlowSegment? Segment,
    int? HttpStatus,
    double? QueryPointToMatchedMeters,
    double? MedianMatchedToOsmMeters,
    double? FractionWithinTolerance,
    DateTimeOffset ProbedAtUtc)
{
    public double? Ratio =>
        Segment is { FreeFlowSpeedKph: > 0 }
            ? Math.Round((double)Segment.CurrentSpeedKph / Segment.FreeFlowSpeedKph, 4)
            : null;
}

/// <summary>Parâmetros de execução.</summary>
public sealed record ProbeOptions
{
    public BoundingBox Bbox { get; init; } = BoundingBox.RioBranco;

    /// <summary>Espaçamento entre pontos amostrados numa mesma via.</summary>
    public double SpacingMeters { get; init; } = 400;

    /// <summary>Recuo das extremidades, para evitar cruzamentos.</summary>
    public double EdgeMarginMeters { get; init; } = 40;

    /// <summary>
    /// Teto de pontos por nome de rua, para uma via longa não dominar o orçamento.
    ///
    /// Só é atingido por vias longas: o espaçamento já limita as curtas a um único ponto.
    /// Com 4, a espinha do bairro (Ernesto Bianchini, 2,8 km) ficava com um ponto a cada 700 m
    /// — densidade insuficiente justamente onde mais importa ver o congestionamento se propagar.
    /// </summary>
    public int MaxPointsPerStreet { get; init; } = 8;

    /// <summary>Teto absoluto de chamadas à TomTom nesta execução.</summary>
    public int MaxCalls { get; init; } = 300;

    /// <summary>Distância máxima aceitável entre a geometria casada e a do OSM.</summary>
    public double ToleranceMeters { get; init; } = 25;

    /// <summary>Fração mínima de pontos casados dentro da tolerância.</summary>
    public double MinFractionWithinTolerance { get; init; } = 0.70;

    /// <summary>Abaixo disto a leitura é tratada como modelada, não medida.</summary>
    public double MinConfidence { get; init; } = 0.50;

    /// <summary>Requisições por segundo (limite da TomTom é 5).</summary>
    public double RequestsPerSecond { get; init; } = 4;

    /// <summary>Classes de via consideradas. Exclui trilha, calçada e acesso interno.</summary>
    public IReadOnlyList<string> HighwayClasses { get; init; } =
    [
        "motorway", "trunk", "primary", "secondary", "tertiary",
        "unclassified", "residential", "living_street",
    ];

    public string OutputDirectory { get; init; } = Path.Combine("data", "reference");

    /// <summary>Sem isto, nenhuma chamada é feita — só o plano é impresso.</summary>
    public bool Confirmed { get; init; }
}
