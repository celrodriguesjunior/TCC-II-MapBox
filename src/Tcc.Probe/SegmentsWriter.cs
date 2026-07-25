using System.Text.Encodings.Web;
using System.Text.Json;
using Tcc.Domain;

namespace Tcc.Probe;

/// <summary>
/// Promove os candidatos aprovados na sondagem para o catálogo que o coletor consome.
///
/// O que sai daqui é um rascunho: a sondagem prova que existe cobertura, mas não sabe qual via é
/// a espinha do bairro nem qual merece cadência fina. Essa curadoria é manual, feita editando o
/// arquivo — ele é dado de referência, não artefato gerado.
/// </summary>
public static class SegmentsWriter
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    public static async Task WriteAsync(string path, IReadOnlyList<ProbeResult> results, CancellationToken ct)
    {
        var accepted = results.Where(r => r.Verdict == Verdict.Aceito).ToList();

        if (accepted.Count == 0)
        {
            return;
        }

        var segments = accepted
            .OrderBy(r => r.Candidate.StreetName, StringComparer.Ordinal)
            .ThenBy(r => r.Candidate.Id, StringComparer.Ordinal)
            .Select(r => new Segment
            {
                Id = r.Candidate.Id,
                StreetName = r.Candidate.StreetName,
                Highway = r.Candidate.Highway,
                Lat = Math.Round(r.Candidate.QueryPoint.Lat, 7),
                Lon = Math.Round(r.Candidate.QueryPoint.Lon, 7),
                Tier = DefaultTier(r.Candidate.Highway),
                OsmWayId = r.Candidate.OsmWayId,
                ProbeConfidence = r.Segment?.Confidence ?? 0,
            })
            .ToList();

        // Centroide dos segmentos aprovados: um único ponto cobre o bairro inteiro, porque a
        // área tem ~3 km e o tempo é o mesmo em toda ela.
        var catalog = new SegmentCatalog
        {
            ProbedAtUtc = accepted[0].ProbedAtUtc,
            WeatherLat = Math.Round(segments.Average(s => s.Lat), 5),
            WeatherLon = Math.Round(segments.Average(s => s.Lon), 5),
            Segments = segments,
        };

        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(catalog, Options), ct)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Camada inicial, derivada da classificação do OSM. É só um ponto de partida — a espinha do
    /// bairro é conhecimento local, não sai da malha viária.
    /// </summary>
    private static SegmentTier DefaultTier(string highway) => highway switch
    {
        "motorway" or "trunk" or "primary" or "secondary" => SegmentTier.Coletora,
        "tertiary" => SegmentTier.Coletora,
        _ => SegmentTier.Secundaria,
    };
}
