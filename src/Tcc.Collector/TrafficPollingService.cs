using System.Globalization;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tcc.Domain;

namespace Tcc.Collector;

/// <summary>
/// Consulta o fluxo de tráfego dos segmentos monitorados.
///
/// Independente do serviço de clima: agendas próprias, sem estado compartilhado. A junção entre
/// os dois acontece no ETL, por um <c>ASOF JOIN</c> explícito. Em 2022 os dois fluxos eram
/// amarrados na coleta por um truncamento de minutos escondido numa linha, duplicado em dois
/// lugares e documentado em nenhum.
/// </summary>
public sealed class TrafficPollingService(
    SegmentCatalog catalog,
    ITrafficProvider provider,
    IRequestBudget budget,
    IOptions<CollectorOptions> options,
    ILogger<TrafficPollingService> logger) : BackgroundService
{
    private readonly CollectorOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using var writer = new NdjsonWriter(_options.OutputDirectory, "traffic", logger);

        // Cada camada tem sua própria cadência, então cada uma roda em seu próprio laço.
        var loops = catalog.Segments
            .GroupBy(s => s.Tier)
            .Select(g => PollTierAsync(g.Key, [.. g], writer, stoppingToken))
            .ToArray();

        logger.LogInformation(
            "Tráfego: {Segments} segmentos em {Tiers} camadas.",
            catalog.Segments.Count, loops.Length);

        await Task.WhenAll(loops).ConfigureAwait(false);
    }

    private async Task PollTierAsync(
        SegmentTier tier,
        IReadOnlyList<Segment> segments,
        NdjsonWriter writer,
        CancellationToken ct)
    {
        var cadence = TimeSpan.FromMinutes(_options.CadenceFor(tier));

        logger.LogInformation(
            "Camada {Tier}: {Count} segmentos a cada {Minutes} min ({Calls} chamadas/dia).",
            tier, segments.Count, cadence.TotalMinutes,
            (int)(segments.Count * (1440 / cadence.TotalMinutes)));

        while (!ct.IsCancellationRequested)
        {
            var cycleStart = CycleSchedule.AlignToNext(DateTimeOffset.UtcNow, cadence);

            try
            {
                await Task.Delay(cycleStart - DateTimeOffset.UtcNow, ct).ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                return;
            }

            var cycleId = cycleStart.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
            var ok = 0;
            var failed = 0;

            foreach (var segment in segments)
            {
                if (ct.IsCancellationRequested)
                {
                    return;
                }

                var observation = await ReadAsync(segment, cycleId, ct).ConfigureAwait(false);
                await writer.WriteAsync(observation, CancellationToken.None).ConfigureAwait(false);

                if (observation.FetchStatus == "ok")
                {
                    ok++;
                }
                else
                {
                    failed++;
                }
            }

            logger.LogInformation(
                "Ciclo {CycleId} [{Tier}]: {Ok} ok, {Failed} falhas, orçamento restante {Remaining}.",
                cycleId, tier, ok, failed, budget.Remaining);
        }
    }

    /// <summary>Campos que identificam a leitura, presentes tanto no sucesso quanto na falha.</summary>
    private readonly record struct Identity(
        string CycleId, string SegmentId, string StreetName, string Tier, double Lat, double Lon);

    private async Task<TrafficObservation> ReadAsync(Segment segment, string cycleId, CancellationToken ct)
    {
        var id = new Identity(
            cycleId, segment.Id, segment.StreetName, segment.Tier.ToString(), segment.Lat, segment.Lon);

        try
        {
            var reading = await provider.GetFlowAsync(segment.Point, ct).ConfigureAwait(false);

            // O deslocamento entre o ponto pedido e o segmento casado é gravado a cada leitura:
            // se a TomTom atualizar o mapa e passar a encaixar noutra via, a série muda de
            // significado, e sem esta métrica isso passaria despercebido.
            double? offset = reading.MatchedGeometry.Count > 0
                ? Math.Round(Geo.DistanceToPolyline(segment.Point, reading.MatchedGeometry), 1)
                : null;

            return new TrafficObservation
            {
                CycleId = id.CycleId,
                SegmentId = id.SegmentId,
                StreetName = id.StreetName,
                Tier = id.Tier,
                Lat = id.Lat,
                Lon = id.Lon,
                FetchedAtUtc = DateTimeOffset.UtcNow,
                FetchStatus = "ok",
                HttpStatus = 200,
                CurrentSpeedKph = reading.CurrentSpeedKph,
                FreeFlowSpeedKph = reading.FreeFlowSpeedKph,
                CurrentTravelTimeSeconds = reading.CurrentTravelTimeSeconds,
                FreeFlowTravelTimeSeconds = reading.FreeFlowTravelTimeSeconds,
                Frc = reading.Frc,
                Confidence = reading.Confidence,
                RoadClosure = reading.RoadClosure,
                MatchOffsetMeters = offset,
            };
        }
        catch (BudgetExhaustedException)
        {
            return Failure(id, "budget_exhausted", "orçamento diário esgotado", null);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning("Falha em {Segment}: {Message}", segment.Id, ex.Message);
            return Failure(id, "error", "http", (int?)ex.StatusCode);
        }
        catch (Exception ex) when (ex is TaskCanceledException or TimeoutException)
        {
            logger.LogWarning("Timeout em {Segment}.", segment.Id);
            return Failure(id, "error", "timeout", null);
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.Text.Json.JsonException)
        {
            logger.LogWarning("Resposta inválida em {Segment}: {Message}", segment.Id, ex.Message);
            return Failure(id, "error", "parse", null);
        }
    }

    private static TrafficObservation Failure(Identity id, string status, string kind, int? httpStatus) => new()
    {
        CycleId = id.CycleId,
        SegmentId = id.SegmentId,
        StreetName = id.StreetName,
        Tier = id.Tier,
        Lat = id.Lat,
        Lon = id.Lon,
        FetchedAtUtc = DateTimeOffset.UtcNow,
        FetchStatus = status,
        HttpStatus = httpStatus,
        ErrorKind = kind,
    };

}
