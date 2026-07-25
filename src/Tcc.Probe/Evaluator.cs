using System.Globalization;

namespace Tcc.Probe;

/// <summary>Monta os candidatos a partir da malha do OSM e julga as respostas da TomTom.</summary>
public static class Evaluator
{
    /// <summary>
    /// Gera pontos de amostragem a partir das vias do OSM.
    ///
    /// Vias longas rendem vários pontos (para ver o congestionamento se propagar ao longo do
    /// eixo), mas o teto por nome de rua evita que a espinha do bairro consuma o orçamento
    /// inteiro. As vias mais longas de cada rua têm prioridade.
    /// </summary>
    public static List<Candidate> BuildCandidates(IReadOnlyList<OsmWay> ways, ProbeOptions options)
    {
        var candidates = new List<Candidate>();

        foreach (var street in ways.GroupBy(w => w.Name, StringComparer.Ordinal).OrderBy(g => g.Key, StringComparer.Ordinal))
        {
            var budget = options.MaxPointsPerStreet;

            // Todos os trechos desta rua, para a validação geométrica comparar contra a via
            // inteira e não contra o fragmento de onde o ponto saiu.
            var streetGeometries = street.Select(w => w.Geometry).ToArray();

            foreach (var way in street.OrderByDescending(w => w.LengthMeters))
            {
                if (budget <= 0)
                {
                    break;
                }

                var points = Geo.SampleAlong(
                    way.Geometry,
                    options.SpacingMeters,
                    options.EdgeMarginMeters,
                    budget);

                foreach (var point in points)
                {
                    var id = string.Create(
                        CultureInfo.InvariantCulture,
                        $"{Slug(street.Key)}-{way.Id}-{candidates.Count:D3}");

                    candidates.Add(new Candidate(
                        id, street.Key, way.Highway, way.Id, point, way.Geometry, streetGeometries));
                    budget--;
                }
            }
        }

        return candidates;
    }

    /// <summary>Julga uma resposta da TomTom contra a geometria do OSM.</summary>
    public static ProbeResult Evaluate(Candidate candidate, FlowResponse response, ProbeOptions options)
    {
        var now = DateTimeOffset.UtcNow;

        if (response.Error is not null || response.Segment is null)
        {
            // Distinguir "não há dado aqui" de "a requisição falhou" é o ponto central desta
            // ferramenta. Tratar 401/403/429/5xx como ausência de cobertura produziria
            // exatamente a falha silenciosa que o coletor de 2022 tinha: um mapa de lacunas
            // que na verdade são erros.
            var verdict = response.HttpStatus switch
            {
                200 => Verdict.SemCobertura,   // respondeu, mas sem flowSegmentData
                404 => Verdict.SemCobertura,   // não há segmento neste ponto
                _ => Verdict.Erro,             // auth, cota, servidor, rede
            };

            var reason = response.HttpStatus switch
            {
                401 or 403 => $"HTTP {response.HttpStatus} — credencial inválida ou sem acesso ao Traffic API",
                429 => "HTTP 429 — cota ou taxa excedida",
                0 => $"falha de transporte: {response.Error}",
                _ => response.Error ?? "resposta sem flowSegmentData",
            };

            return new ProbeResult(candidate, verdict, reason, null,
                response.HttpStatus == 0 ? null : response.HttpStatus, null, null, null, now);
        }

        var segment = response.Segment;

        if (segment.MatchedGeometry.Count == 0)
        {
            return new ProbeResult(candidate, Verdict.SemCobertura,
                "a TomTom não retornou geometria do segmento casado", segment,
                response.HttpStatus, null, null, null, now);
        }

        // Primeiro teste: a TomTom respondeu sobre o lugar pedido?
        var queryToMatched = Geo.DistanceToPolyline(candidate.QueryPoint, segment.MatchedGeometry);

        if (queryToMatched > options.MaxQueryToMatchedMeters)
        {
            var dist = queryToMatched.ToString("F0", CultureInfo.InvariantCulture);
            var max = options.MaxQueryToMatchedMeters.ToString("F0", CultureInfo.InvariantCulture);

            return new ProbeResult(candidate, Verdict.GeometriaDivergente,
                $"o segmento casado está a {dist} m do ponto consultado (máximo {max} m)",
                segment, response.HttpStatus, queryToMatched, null, null, now);
        }

        // Segundo teste: nas imediações do ponto, a geometria casada acompanha a via pedida?
        //
        // Só a vizinhança é avaliada. A TomTom devolve o segmento inteiro em que o ponto cai, e
        // ele frequentemente extrapola o recorte — medir a extensão toda reprovaria casamentos
        // corretos apenas por serem longos. E a comparação é contra todos os trechos da rua,
        // porque no OSM uma via vem partida em vários ways.
        // A amostragem é feita ao longo da linha, não sobre seus vértices: assim o veredito não
        // depende de quantos vértices o provedor resolveu devolver. Uma via transversal que só
        // toca o ponto no cruzamento é reprovada, porque as amostras vizinhas se afastam da rua.
        var centerAlong = Geo.DistanceAlongToClosest(segment.MatchedGeometry, candidate.QueryPoint);
        var local = Geo.SampleWindow(
            segment.MatchedGeometry, centerAlong, options.LocalWindowMeters, options.SampleStepMeters);

        var distances = local
            .Select(p => Geo.DistanceToPolylines(p, candidate.StreetGeometries))
            .OrderBy(d => d)
            .ToArray();

        var median = distances.Length % 2 == 1
            ? distances[distances.Length / 2]
            : (distances[(distances.Length / 2) - 1] + distances[distances.Length / 2]) / 2;

        var within = distances.Count(d => d <= options.ToleranceMeters) / (double)distances.Length;

        if (within < options.MinFractionWithinTolerance)
        {
            var pct = within.ToString("P0", CultureInfo.InvariantCulture);
            var tol = options.ToleranceMeters.ToString("F0", CultureInfo.InvariantCulture);
            var med = median.ToString("F0", CultureInfo.InvariantCulture);
            var win = options.LocalWindowMeters.ToString("F0", CultureInfo.InvariantCulture);
            var reason =
                $"nos {win} m em torno do ponto, apenas {pct} da geometria casada está a até {tol} m " +
                $"da rua pedida (mediana {med} m) — provavelmente é a via transversal";

            return new ProbeResult(candidate, Verdict.GeometriaDivergente, reason, segment,
                response.HttpStatus, queryToMatched, median, within, now);
        }

        if (segment.Confidence < options.MinConfidence)
        {
            var got = segment.Confidence.ToString("F2", CultureInfo.InvariantCulture);
            var min = options.MinConfidence.ToString("F2", CultureInfo.InvariantCulture);
            var reason =
                $"geometria compatível, mas confidence {got} < {min} " +
                "— leitura provavelmente modelada, não medida";

            return new ProbeResult(candidate, Verdict.ConfiancaBaixa, reason, segment,
                response.HttpStatus, queryToMatched, median, within, now);
        }

        var okPct = within.ToString("P0", CultureInfo.InvariantCulture);
        var okTol = options.ToleranceMeters.ToString("F0", CultureInfo.InvariantCulture);
        var okConf = segment.Confidence.ToString("F2", CultureInfo.InvariantCulture);
        var ok = $"geometria compatível ({okPct} dentro de {okTol} m), confidence {okConf}";

        return new ProbeResult(candidate, Verdict.Aceito, ok, segment,
            response.HttpStatus, queryToMatched, median, within, now);
    }

    private static string Slug(string value)
    {
        var normalized = value.Normalize(System.Text.NormalizationForm.FormD);
        var chars = normalized
            .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c)
                        != System.Globalization.UnicodeCategory.NonSpacingMark)
            .Select(c => char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : '-')
            .ToArray();

        var slug = new string(chars);
        while (slug.Contains("--", StringComparison.Ordinal))
        {
            slug = slug.Replace("--", "-", StringComparison.Ordinal);
        }

        return slug.Trim('-');
    }
}
