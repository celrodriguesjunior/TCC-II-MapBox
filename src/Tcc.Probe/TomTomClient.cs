using Tcc.Domain;
using System.Globalization;
using System.Text.Json;

namespace Tcc.Probe;

/// <summary>Resposta crua de uma consulta ao flowSegmentData, preservada para auditoria.</summary>
public sealed record FlowResponse(int HttpStatus, string? RawBody, FlowSegment? Segment, string? Error)
{
    /// <summary>
    /// Falhas que não adianta repetir ponto a ponto — credencial inválida ou cota estourada.
    /// Diante delas a sondagem deve abortar, não seguir gastando chamadas.
    /// </summary>
    public bool IsFatal => HttpStatus is 401 or 403 or 429;
}

/// <summary>
/// Cliente do TomTom Traffic API v4 — Flow Segment Data.
///
/// Verificado em jul/2026: a v4 continua ativa e não depreciada. O plano gratuito permite 2.500
/// requisições não-tile por dia e 5 por segundo.
/// </summary>
public sealed class TomTomClient(HttpClient http, string apiKey)
{
    private const string BaseUrl = "https://api.tomtom.com/traffic/services/4/flowSegmentData/absolute/10/json";

    public async Task<FlowResponse> GetFlowAsync(GeoPoint point, CancellationToken ct)
    {
        var url = string.Create(
            CultureInfo.InvariantCulture,
            $"{BaseUrl}?key={Uri.EscapeDataString(apiKey)}&unit=KMPH&point={point}");

        try
        {
            using var response = await http.GetAsync(url, ct).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            var status = (int)response.StatusCode;

            if (!response.IsSuccessStatusCode)
            {
                return new FlowResponse(status, body, null, $"HTTP {status}");
            }

            return new FlowResponse(status, body, Parse(body), null);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return new FlowResponse(0, null, null, ex.Message);
        }
    }

    private static FlowSegment? Parse(string body)
    {
        using var doc = JsonDocument.Parse(body);

        if (!doc.RootElement.TryGetProperty("flowSegmentData", out var data))
        {
            return null;
        }

        var matched = new List<GeoPoint>();
        if (data.TryGetProperty("coordinates", out var coords)
            && coords.TryGetProperty("coordinate", out var list))
        {
            matched.AddRange(list.EnumerateArray().Select(c =>
                new GeoPoint(c.GetProperty("latitude").GetDouble(), c.GetProperty("longitude").GetDouble())));
        }

        return new FlowSegment(
            Frc: GetString(data, "frc") ?? "",
            CurrentSpeedKph: GetInt(data, "currentSpeed"),
            FreeFlowSpeedKph: GetInt(data, "freeFlowSpeed"),
            CurrentTravelTimeSeconds: GetInt(data, "currentTravelTime"),
            FreeFlowTravelTimeSeconds: GetInt(data, "freeFlowTravelTime"),
            Confidence: GetDouble(data, "confidence"),
            RoadClosure: data.TryGetProperty("roadClosure", out var rc)
                         && rc.ValueKind == JsonValueKind.True,
            MatchedGeometry: matched);
    }

    private static string? GetString(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static int GetInt(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetInt32() : 0;

    private static double GetDouble(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDouble() : 0;
}
