using System.Globalization;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tcc.Domain;

namespace Tcc.Collector;

/// <summary>
/// Consulta o clima num único ponto do bairro.
///
/// Um ponto basta: Rio Branco tem ~3 km de ponta a ponta e o tempo é o mesmo em toda a área. O
/// coletor de 2022 consultava clima nos 33 pontos de tráfego — 4.752 chamadas por dia para obter
/// 144 informações distintas.
///
/// Não consome o orçamento da TomTom: a Open-Meteo é gratuita para uso não comercial e não exige
/// chave, então roda no seu próprio <c>HttpClient</c>, sem o handler de orçamento.
/// </summary>
public sealed class WeatherPollingService(
    SegmentCatalog catalog,
    IWeatherProvider provider,
    IOptions<CollectorOptions> options,
    ILogger<WeatherPollingService> logger) : BackgroundService
{
    private readonly CollectorOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using var writer = new NdjsonWriter(_options.OutputDirectory, "weather", logger);

        var point = new GeoPoint(catalog.WeatherLat, catalog.WeatherLon);
        var cadence = TimeSpan.FromMinutes(_options.WeatherCadenceMinutes);

        logger.LogInformation(
            "Clima: ponto {Point} a cada {Minutes} min ({Calls} chamadas/dia).",
            point, cadence.TotalMinutes, (int)(1440 / cadence.TotalMinutes));

        while (!stoppingToken.IsCancellationRequested)
        {
            var cycleStart = CycleSchedule.AlignToNext(DateTimeOffset.UtcNow, cadence);

            try
            {
                await Task.Delay(cycleStart - DateTimeOffset.UtcNow, stoppingToken).ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                return;
            }

            var cycleId = cycleStart.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
            var observation = await ReadAsync(point, cycleId, stoppingToken).ConfigureAwait(false);

            await writer.WriteAsync(observation, CancellationToken.None).ConfigureAwait(false);

            if (observation.FetchStatus == "ok")
            {
                logger.LogInformation(
                    "Clima {CycleId}: WMO {Code}, {Temp}°C, precipitação {Rain} mm.",
                    cycleId, observation.WeatherCodeWmo, observation.TemperatureC, observation.PrecipitationMm);
            }
            else
            {
                logger.LogWarning("Clima {CycleId}: falha ({Kind}).", cycleId, observation.ErrorKind);
            }
        }
    }

    private async Task<WeatherObservation> ReadAsync(GeoPoint point, string cycleId, CancellationToken ct)
    {
        try
        {
            var reading = await provider.GetCurrentAsync(point, ct).ConfigureAwait(false);

            return new WeatherObservation
            {
                CycleId = cycleId,
                Lat = point.Lat,
                Lon = point.Lon,
                FetchedAtUtc = DateTimeOffset.UtcNow,
                ObservedAtUtc = reading.ObservedAtUtc,
                FetchStatus = "ok",
                HttpStatus = 200,
                WeatherCodeWmo = reading.WeatherCodeWmo,
                TemperatureC = reading.TemperatureC,
                ApparentTemperatureC = reading.ApparentTemperatureC,
                RelativeHumidityPct = reading.RelativeHumidityPct,
                PrecipitationMm = reading.PrecipitationMm,
                RainMm = reading.RainMm,
                WindSpeedKph = reading.WindSpeedKph,
                WindGustsKph = reading.WindGustsKph,
                CloudCoverPct = reading.CloudCoverPct,
                IsDay = reading.IsDay,
            };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException
                                       or TimeoutException or InvalidOperationException
                                       or System.Text.Json.JsonException)
        {
            // Falha de clima é anotada, não silenciada — e, ao contrário de 2022, é recuperável:
            // a Open-Meteo tem arquivo histórico, então o ETL pode preencher a lacuna depois.
            var kind = ex switch
            {
                HttpRequestException => "http",
                TaskCanceledException or TimeoutException => "timeout",
                _ => "parse",
            };

            return new WeatherObservation
            {
                CycleId = cycleId,
                Lat = point.Lat,
                Lon = point.Lon,
                FetchedAtUtc = DateTimeOffset.UtcNow,
                FetchStatus = "error",
                HttpStatus = (ex as HttpRequestException)?.StatusCode is { } s ? (int)s : null,
                ErrorKind = kind,
            };
        }
    }
}
