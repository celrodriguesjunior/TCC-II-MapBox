using System.Net.Http.Headers;
using System.Text.Json;

namespace Tcc.Probe;

/// <summary>Busca a malha viária nomeada no OpenStreetMap via Overpass API.</summary>
public sealed class OverpassClient(HttpClient http)
{
    private const string Endpoint = "https://overpass-api.de/api/interpreter";

    /// <summary>
    /// O Overpass é um serviço público compartilhado e devolve 429/504 com alguma frequência
    /// quando está sob carga. Como a sondagem inteira depende desta consulta, vale insistir
    /// com espera crescente em vez de abortar.
    /// </summary>
    private static readonly int[] RetryDelaysSeconds = [5, 15, 40];

    public async Task<IReadOnlyList<OsmWay>> FetchNamedWaysAsync(
        BoundingBox bbox,
        IReadOnlyList<string> highwayClasses,
        CancellationToken ct)
    {
        var classes = string.Join('|', highwayClasses);
        var query =
            $"""
             [out:json][timeout:180];
             way["highway"~"^({classes})$"]["name"]({bbox.ToOverpass()});
             out tags geom;
             """;

        string body;
        var attempt = 0;

        while (true)
        {
            using var content = new FormUrlEncodedContent([new KeyValuePair<string, string>("data", query)]);
            using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint) { Content = content };
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue("TCC-Probe", "1.0"));

            using var response = await http.SendAsync(request, ct).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                break;
            }

            var transient = (int)response.StatusCode is 429 or 502 or 503 or 504;
            if (!transient || attempt >= RetryDelaysSeconds.Length)
            {
                response.EnsureSuccessStatusCode();
                throw new HttpRequestException($"Overpass respondeu {(int)response.StatusCode}.");
            }

            var delay = RetryDelaysSeconds[attempt++];
            Console.WriteLine($"  Overpass respondeu {(int)response.StatusCode}; nova tentativa em {delay}s...");
            await Task.Delay(TimeSpan.FromSeconds(delay), ct).ConfigureAwait(false);
        }

        using var doc = JsonDocument.Parse(body);

        var ways = new List<OsmWay>();

        foreach (var element in doc.RootElement.GetProperty("elements").EnumerateArray())
        {
            if (!element.TryGetProperty("geometry", out var geometry))
            {
                continue;
            }

            var tags = element.GetProperty("tags");
            var name = tags.TryGetProperty("name", out var n) ? n.GetString() : null;
            var highway = tags.TryGetProperty("highway", out var h) ? h.GetString() : null;

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(highway))
            {
                continue;
            }

            var points = geometry.EnumerateArray()
                .Select(p => new GeoPoint(p.GetProperty("lat").GetDouble(), p.GetProperty("lon").GetDouble()))
                .ToList();

            if (points.Count < 2)
            {
                continue;
            }

            var oneway = tags.TryGetProperty("oneway", out var o)
                         && string.Equals(o.GetString(), "yes", StringComparison.OrdinalIgnoreCase);

            ways.Add(new OsmWay(element.GetProperty("id").GetInt64(), name, highway, oneway, points));
        }

        return ways;
    }
}
