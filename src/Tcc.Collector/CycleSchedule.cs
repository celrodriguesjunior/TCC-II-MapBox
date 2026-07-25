namespace Tcc.Collector;

/// <summary>
/// Grade temporal da coleta.
///
/// Ciclos alinhados a partir da meia-noite UTC, em vez de contados a partir do instante em que o
/// processo subiu. Isso garante duas coisas que o ETL depende: todos os segmentos de uma camada
/// compartilham o mesmo <c>cycle_id</c>, e a grade permanece a mesma depois de um reinício — sem
/// isso, cada reinício deslocaria a série e a comparação entre dias ficaria torta.
/// </summary>
public static class CycleSchedule
{
    /// <summary>Próximo instante da grade estritamente após <paramref name="now"/>.</summary>
    public static DateTimeOffset AlignToNext(DateTimeOffset now, TimeSpan cadence)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(cadence, TimeSpan.Zero);

        var utc = now.ToUniversalTime();
        var dayStart = new DateTimeOffset(utc.Date, TimeSpan.Zero);
        var periods = ((utc - dayStart).Ticks / cadence.Ticks) + 1;

        return dayStart.AddTicks(periods * cadence.Ticks);
    }
}
