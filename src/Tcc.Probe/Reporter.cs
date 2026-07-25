using Tcc.Domain;
using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Tcc.Probe;

/// <summary>Escreve os artefatos da sondagem: GeoJSON para inspeção visual, NDJSON para auditoria e um resumo legível.</summary>
public static class Reporter
{
    private static readonly JsonSerializerOptions GeoJsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>
    /// GeoJSON com duas feições por candidato: o ponto consultado e a geometria que a TomTom
    /// casou. Abrir num mapa e comparar visualmente é o jeito mais rápido de flagrar encaixe
    /// errado — e é onde o conhecimento local do bairro vale mais que qualquer limiar numérico.
    /// </summary>
    public static async Task WriteGeoJsonAsync(string path, IReadOnlyList<ProbeResult> results, CancellationToken ct)
    {
        var features = new List<object>();

        foreach (var r in results)
        {
            features.Add(new
            {
                type = "Feature",
                id = r.Candidate.Id,
                geometry = new
                {
                    type = "Point",
                    coordinates = new[] { r.Candidate.QueryPoint.Lon, r.Candidate.QueryPoint.Lat },
                },
                properties = new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["kind"] = "query_point",
                    ["id"] = r.Candidate.Id,
                    ["street"] = r.Candidate.StreetName,
                    ["highway"] = r.Candidate.Highway,
                    ["osm_way_id"] = r.Candidate.OsmWayId,
                    ["verdict"] = r.Verdict.ToString(),
                    ["reason"] = r.Reason,
                    ["http_status"] = r.HttpStatus,
                    ["frc"] = r.Segment?.Frc,
                    ["current_speed_kph"] = r.Segment?.CurrentSpeedKph,
                    ["free_flow_speed_kph"] = r.Segment?.FreeFlowSpeedKph,
                    ["ratio"] = r.Ratio,
                    ["confidence"] = r.Segment?.Confidence,
                    ["road_closure"] = r.Segment?.RoadClosure,
                    ["query_to_matched_m"] = Round(r.QueryPointToMatchedMeters),
                    ["median_matched_to_osm_m"] = Round(r.MedianMatchedToOsmMeters),
                    ["fraction_within_tolerance"] = Round(r.FractionWithinTolerance, 3),
                    ["probed_at_utc"] = r.ProbedAtUtc.ToString("O", CultureInfo.InvariantCulture),
                },
            });

            if (r.Segment is { MatchedGeometry.Count: >= 2 })
            {
                features.Add(new
                {
                    type = "Feature",
                    id = $"{r.Candidate.Id}-matched",
                    geometry = new
                    {
                        type = "LineString",
                        coordinates = r.Segment.MatchedGeometry.Select(p => new[] { p.Lon, p.Lat }).ToArray(),
                    },
                    properties = new Dictionary<string, object?>(StringComparer.Ordinal)
                    {
                        ["kind"] = "matched_segment",
                        ["id"] = r.Candidate.Id,
                        ["street"] = r.Candidate.StreetName,
                        ["verdict"] = r.Verdict.ToString(),
                    },
                });
            }
        }

        var collection = new { type = "FeatureCollection", features };
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(collection, GeoJsonOptions), ct)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// GeoJSON dos candidatos antes de qualquer consulta à TomTom: os pontos que seriam
    /// amostrados e a via de origem de cada um.
    ///
    /// Serve para revisar a seleção contra conhecimento local do bairro — se um ponto caiu numa
    /// rua onde não passa carro, ou se falta a rua onde sempre trava, isso aparece aqui, antes
    /// de gastar orçamento.
    /// </summary>
    public static async Task WriteCandidatesGeoJsonAsync(
        string path,
        IReadOnlyList<Candidate> candidates,
        CancellationToken ct)
    {
        var features = new List<object>();

        foreach (var c in candidates)
        {
            features.Add(new
            {
                type = "Feature",
                id = c.Id,
                geometry = new { type = "Point", coordinates = new[] { c.QueryPoint.Lon, c.QueryPoint.Lat } },
                properties = new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["kind"] = "query_point",
                    ["id"] = c.Id,
                    ["street"] = c.StreetName,
                    ["highway"] = c.Highway,
                    ["osm_way_id"] = c.OsmWayId,
                },
            });
        }

        // Uma feição por via, sem repetir a geometria a cada ponto amostrado nela.
        foreach (var way in candidates.GroupBy(c => c.OsmWayId))
        {
            var first = way.First();
            features.Add(new
            {
                type = "Feature",
                id = $"way-{way.Key}",
                geometry = new
                {
                    type = "LineString",
                    coordinates = first.OsmGeometry.Select(p => new[] { p.Lon, p.Lat }).ToArray(),
                },
                properties = new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["kind"] = "osm_way",
                    ["street"] = first.StreetName,
                    ["highway"] = first.Highway,
                    ["osm_way_id"] = way.Key,
                    ["sample_points"] = way.Count(),
                },
            });
        }

        var collection = new { type = "FeatureCollection", features };
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(collection, GeoJsonOptions), ct)
            .ConfigureAwait(false);
    }

    /// <summary>Uma linha JSON por candidato — registro cru para auditoria e reprocessamento.</summary>
    public static async Task WriteNdjsonAsync(string path, IReadOnlyList<ProbeResult> results, CancellationToken ct)
    {
        var sb = new StringBuilder();

        foreach (var r in results)
        {
            var record = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["schema_version"] = 1,
                ["stage"] = "probe-structural",
                ["id"] = r.Candidate.Id,
                ["street"] = r.Candidate.StreetName,
                ["highway"] = r.Candidate.Highway,
                ["osm_way_id"] = r.Candidate.OsmWayId,
                ["query_lat"] = r.Candidate.QueryPoint.Lat,
                ["query_lon"] = r.Candidate.QueryPoint.Lon,
                ["verdict"] = r.Verdict.ToString(),
                ["reason"] = r.Reason,
                ["http_status"] = r.HttpStatus,
                ["frc"] = r.Segment?.Frc,
                ["current_speed_kph"] = r.Segment?.CurrentSpeedKph,
                ["free_flow_speed_kph"] = r.Segment?.FreeFlowSpeedKph,
                ["current_travel_time_s"] = r.Segment?.CurrentTravelTimeSeconds,
                ["free_flow_travel_time_s"] = r.Segment?.FreeFlowTravelTimeSeconds,
                ["ratio"] = r.Ratio,
                ["confidence"] = r.Segment?.Confidence,
                ["road_closure"] = r.Segment?.RoadClosure,
                ["query_to_matched_m"] = Round(r.QueryPointToMatchedMeters),
                ["median_matched_to_osm_m"] = Round(r.MedianMatchedToOsmMeters),
                ["fraction_within_tolerance"] = Round(r.FractionWithinTolerance, 3),
                ["matched_geometry"] = r.Segment?.MatchedGeometry.Select(p => new[] { p.Lon, p.Lat }).ToArray(),
                ["probed_at_utc"] = r.ProbedAtUtc.ToString("O", CultureInfo.InvariantCulture),
            };

            sb.AppendLine(JsonSerializer.Serialize(record, JsonSerializerOptions.Default));
        }

        await File.WriteAllTextAsync(path, sb.ToString(), ct).ConfigureAwait(false);
    }

    /// <summary>Resumo em Markdown, pronto para virar seção da documentação.</summary>
    public static async Task WriteMarkdownAsync(
        string path,
        IReadOnlyList<ProbeResult> results,
        ProbeOptions options,
        CancellationToken ct)
    {
        var sb = new StringBuilder();
        var accepted = results.Where(r => r.Verdict == Verdict.Aceito).ToList();

        sb.AppendLine("# Sondagem Estrutural — Rio Branco, Brusque/SC");
        sb.AppendLine();
        sb.AppendLine(CultureInfo.InvariantCulture, $"**Executada em:** {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm} UTC");
        sb.AppendLine(CultureInfo.InvariantCulture, $"**Candidatos sondados:** {results.Count}");

        var tol = options.ToleranceMeters.ToString("F0", CultureInfo.InvariantCulture);
        var minFrac = options.MinFractionWithinTolerance.ToString("P0", CultureInfo.InvariantCulture);
        sb.AppendLine($"**Tolerância geométrica:** {tol} m (mínimo {minFrac} dos pontos dentro)");
        sb.AppendLine(CultureInfo.InvariantCulture, $"**Confiança mínima:** {options.MinConfidence:F2}");
        sb.AppendLine();

        sb.AppendLine("## Veredito");
        sb.AppendLine();
        sb.AppendLine("| Veredito | Candidatos | % |");
        sb.AppendLine("|---|---:|---:|");
        foreach (var group in results.GroupBy(r => r.Verdict).OrderByDescending(g => g.Count()))
        {
            var pct = (double)group.Count() / results.Count;
            sb.AppendLine(CultureInfo.InvariantCulture, $"| {group.Key} | {group.Count()} | {pct:P0} |");
        }
        sb.AppendLine();

        var streets = accepted.Select(r => r.Candidate.StreetName).Distinct(StringComparer.Ordinal).Count();
        sb.AppendLine(CultureInfo.InvariantCulture,
            $"**{accepted.Count} pontos aceitos, cobrindo {streets} ruas distintas.**");
        sb.AppendLine();

        if (accepted.Count > 0)
        {
            var cycles = (int)(2400.0 / accepted.Count);
            var cadence = (1440.0 / cycles).ToString("F1", CultureInfo.InvariantCulture);
            sb.AppendLine("### Cadência viável com os aceitos");
            sb.AppendLine();
            sb.AppendLine(
                $"Com {accepted.Count} segmentos e o teto operacional de 2.400 chamadas/dia: " +
                $"**{cycles} ciclos/dia, ou um a cada {cadence} minutos** em grade uniforme.");
            sb.AppendLine();
        }

        sb.AppendLine("## Aceitos");
        sb.AppendLine();
        sb.AppendLine("| ID | Rua | Classe | FRC | Atual | Livre | Ratio | Conf. | Desvio mediano |");
        sb.AppendLine("|---|---|---|---|---:|---:|---:|---:|---:|");
        foreach (var r in accepted.OrderBy(r => r.Candidate.StreetName, StringComparer.Ordinal))
        {
            var ratio = r.Ratio?.ToString("F2", CultureInfo.InvariantCulture) ?? "-";
            var conf = r.Segment?.Confidence.ToString("F2", CultureInfo.InvariantCulture) ?? "-";
            var dev = r.MedianMatchedToOsmMeters?.ToString("F0", CultureInfo.InvariantCulture) ?? "-";

            sb.AppendLine(
                $"| `{r.Candidate.Id}` | {r.Candidate.StreetName} | {r.Candidate.Highway} | {r.Segment?.Frc} | " +
                $"{r.Segment?.CurrentSpeedKph} | {r.Segment?.FreeFlowSpeedKph} | {ratio} | {conf} | {dev} m |");
        }
        sb.AppendLine();

        foreach (var verdict in new[] { Verdict.GeometriaDivergente, Verdict.ConfiancaBaixa, Verdict.SemCobertura, Verdict.Erro })
        {
            var group = results.Where(r => r.Verdict == verdict).ToList();
            if (group.Count == 0)
            {
                continue;
            }

            sb.AppendLine(CultureInfo.InvariantCulture, $"## {verdict} ({group.Count})");
            sb.AppendLine();

            if (verdict == Verdict.GeometriaDivergente)
            {
                sb.AppendLine("> A API respondeu com sucesso, mas sobre outra via. É a falha mais perigosa:");
                sb.AppendLine("> HTTP 200, número plausível, dado errado. Descartar.");
                sb.AppendLine();
            }

            sb.AppendLine("| ID | Rua | Classe | Motivo |");
            sb.AppendLine("|---|---|---|---|");
            foreach (var r in group.OrderBy(r => r.Candidate.StreetName, StringComparer.Ordinal))
            {
                sb.AppendLine(
                    $"| `{r.Candidate.Id}` | {r.Candidate.StreetName} | {r.Candidate.Highway} | {r.Reason} |");
            }
            sb.AppendLine();
        }

        sb.AppendLine("## Próximo passo");
        sb.AppendLine();
        sb.AppendLine("Esta sondagem é **estrutural** — mede se existe dado, não se o dado varia.");
        sb.AppendLine("Ruas aceitas que permaneçam em `ratio = 1,00` ao longo de uma semana não têm sinal");
        sb.AppendLine("e devem sair da lista definitiva. Isso é a sondagem temporal.");
        sb.AppendLine();
        sb.AppendLine("Antes disso: abrir `candidates.geojson` num mapa e conferir os pontos aceitos contra");
        sb.AppendLine("o conhecimento local do bairro. Um ponto aceito numa rua onde não passa carro indica");
        sb.AppendLine("problema no método, não cobertura boa.");

        await File.WriteAllTextAsync(path, sb.ToString(), ct).ConfigureAwait(false);
    }

    private static double? Round(double? value, int digits = 1) =>
        value is null || double.IsInfinity(value.Value) ? null : Math.Round(value.Value, digits);
}
