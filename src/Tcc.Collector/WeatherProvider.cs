using System.Globalization;
using System.Text.Json;
using Tcc.Domain;

namespace Tcc.Collector;

/// <summary>Leitura meteorológica normalizada.</summary>
public sealed record WeatherReading(
    DateTimeOffset? ObservedAtUtc,
    int? WeatherCodeWmo,
    double? TemperatureC,
    double? ApparentTemperatureC,
    int? RelativeHumidityPct,
    double? PrecipitationMm,
    double? RainMm,
    double? WindSpeedKph,
    double? WindGustsKph,
    int? CloudCoverPct,
    bool? IsDay);

public interface IWeatherProvider
{
    Task<WeatherReading> GetCurrentAsync(GeoPoint point, CancellationToken ct);
}

/// <summary>
/// Open-Meteo, endpoint de condições atuais.
///
/// Substitui a OpenWeather do TCC de 2022, que deixou de ser viável no plano gratuito: a
/// One Call 2.5 foi depreciada em jun/2024 e a 3.0 exige cartão cadastrado. A Open-Meteo não
/// pede chave para uso não comercial e devolve códigos WMO padronizados — em 2022 o que se
/// guardava era a descrição em português, mapeada para um inteiro por uma tabela que só existia
/// dentro de um <c>&lt;select&gt;</c> do HTML.
///
/// Lacunas aqui são recuperáveis: a mesma API tem arquivo histórico, então um período em que o
/// coletor caiu pode ser preenchido depois no ETL. Em 2022, 9% dos registros ficaram sem clima
/// para sempre.
/// </summary>
public sealed class OpenMeteoWeatherProvider(HttpClient http) : IWeatherProvider
{
    private const string BaseUrl = "https://api.open-meteo.com/v1/forecast";

    private static readonly string[] Fields =
    [
        "temperature_2m", "apparent_temperature", "relative_humidity_2m",
        "precipitation", "rain", "weather_code", "cloud_cover",
        "wind_speed_10m", "wind_gusts_10m", "is_day",
    ];

    public async Task<WeatherReading> GetCurrentAsync(GeoPoint point, CancellationToken ct)
    {
        var url = string.Create(
            CultureInfo.InvariantCulture,
            $"{BaseUrl}?latitude={point.Lat:F5}&longitude={point.Lon:F5}" +
            $"&current={string.Join(',', Fields)}&wind_speed_unit=kmh&timezone=UTC");

        using var response = await http.GetAsync(url, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        using var doc = JsonDocument.Parse(body);

        if (!doc.RootElement.TryGetProperty("current", out var c))
        {
            throw new InvalidOperationException("Resposta sem o bloco 'current'.");
        }

        return new WeatherReading(
            ObservedAtUtc: ParseTime(c),
            WeatherCodeWmo: Int(c, "weather_code"),
            TemperatureC: Double(c, "temperature_2m"),
            ApparentTemperatureC: Double(c, "apparent_temperature"),
            RelativeHumidityPct: Int(c, "relative_humidity_2m"),
            PrecipitationMm: Double(c, "precipitation"),
            RainMm: Double(c, "rain"),
            WindSpeedKph: Double(c, "wind_speed_10m"),
            WindGustsKph: Double(c, "wind_gusts_10m"),
            CloudCoverPct: Int(c, "cloud_cover"),
            IsDay: Int(c, "is_day") is { } d ? d == 1 : null);
    }

    private static DateTimeOffset? ParseTime(JsonElement current)
    {
        if (!current.TryGetProperty("time", out var t) || t.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        // A API devolve tempo local do fuso pedido, sem offset. Pedimos timezone=UTC.
        return DateTime.TryParse(
            t.GetString(), CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsed)
            ? new DateTimeOffset(parsed, TimeSpan.Zero)
            : null;
    }

    private static int? Int(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetInt32() : null;

    private static double? Double(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDouble() : null;
}
