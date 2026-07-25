using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Tcc.Domain;

namespace Tcc.Collector;

/// <summary>Leitura de fluxo devolvida pelo provedor, já normalizada.</summary>
public sealed record FlowReading(
    int CurrentSpeedKph,
    int FreeFlowSpeedKph,
    int CurrentTravelTimeSeconds,
    int FreeFlowTravelTimeSeconds,
    string Frc,
    double Confidence,
    bool RoadClosure,
    IReadOnlyList<GeoPoint> MatchedGeometry);

/// <summary>
/// Abstrai o provedor de fluxo. A TomTom é a escolha atual, mas a cota gratuita é apertada e os
/// termos mudam — trocar de provedor deve custar um arquivo, não o coletor inteiro.
/// </summary>
public interface ITrafficProvider
{
    Task<FlowReading> GetFlowAsync(GeoPoint point, CancellationToken ct);
}

public sealed class TomTomTrafficProvider(HttpClient http, IOptions<CollectorOptions> options) : ITrafficProvider
{
    private readonly TomTomOptions _tomtom = options.Value.TomTom;

    public async Task<FlowReading> GetFlowAsync(GeoPoint point, CancellationToken ct)
    {
        var url = string.Create(
            CultureInfo.InvariantCulture,
            $"{_tomtom.BaseUrl}?key={Uri.EscapeDataString(_tomtom.ApiKey!)}&unit=KMPH&point={point}");

        using var response = await http.GetAsync(url, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        using var doc = JsonDocument.Parse(body);

        if (!doc.RootElement.TryGetProperty("flowSegmentData", out var data))
        {
            throw new InvalidOperationException("Resposta sem flowSegmentData.");
        }

        var matched = new List<GeoPoint>();
        if (data.TryGetProperty("coordinates", out var coords)
            && coords.TryGetProperty("coordinate", out var list))
        {
            matched.AddRange(list.EnumerateArray().Select(c =>
                new GeoPoint(c.GetProperty("latitude").GetDouble(), c.GetProperty("longitude").GetDouble())));
        }

        return new FlowReading(
            CurrentSpeedKph: GetInt(data, "currentSpeed"),
            FreeFlowSpeedKph: GetInt(data, "freeFlowSpeed"),
            CurrentTravelTimeSeconds: GetInt(data, "currentTravelTime"),
            FreeFlowTravelTimeSeconds: GetInt(data, "freeFlowTravelTime"),
            Frc: GetString(data, "frc") ?? "",
            Confidence: GetDouble(data, "confidence"),
            RoadClosure: data.TryGetProperty("roadClosure", out var rc) && rc.ValueKind == JsonValueKind.True,
            MatchedGeometry: matched);
    }

    private static string? GetString(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static int GetInt(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetInt32() : 0;

    private static double GetDouble(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDouble() : 0;
}
