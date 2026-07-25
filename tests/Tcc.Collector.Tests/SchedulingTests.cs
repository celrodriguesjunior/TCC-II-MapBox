using Tcc.Collector;
using Xunit;

namespace Tcc.Collector.Tests;

public class SchedulingTests
{
    [Theory]
    [InlineData("2026-07-25T03:37:12Z", 10, "2026-07-25T03:40:00Z")]
    [InlineData("2026-07-25T03:40:00Z", 10, "2026-07-25T03:50:00Z")]
    [InlineData("2026-07-25T03:41:00Z", 30, "2026-07-25T04:00:00Z")]
    [InlineData("2026-07-25T00:00:01Z", 90, "2026-07-25T01:30:00Z")]
    public void AlignToNext_CaiSempreNaGrade(string agora, int cadenciaMin, string esperado)
    {
        var resultado = CycleSchedule.AlignToNext(
            DateTimeOffset.Parse(agora, System.Globalization.CultureInfo.InvariantCulture),
            TimeSpan.FromMinutes(cadenciaMin));

        Assert.Equal(
            DateTimeOffset.Parse(esperado, System.Globalization.CultureInfo.InvariantCulture),
            resultado);
    }

    [Fact]
    public void AlignToNext_EhSempreFuturo()
    {
        // Devolver o instante atual faria o laço disparar em rajada em vez de esperar o ciclo.
        var agora = DateTimeOffset.UtcNow;

        foreach (var minutos in new[] { 5, 10, 20, 30, 60, 90 })
        {
            var proximo = CycleSchedule.AlignToNext(agora, TimeSpan.FromMinutes(minutos));
            Assert.True(proximo > agora, $"cadência de {minutos} min devolveu instante não futuro");
        }
    }

    [Fact]
    public void AlignToNext_AtravessaAMeiaNoite()
    {
        var quaseMeiaNoite = DateTimeOffset.Parse(
            "2026-07-25T23:52:00Z", System.Globalization.CultureInfo.InvariantCulture);

        var proximo = CycleSchedule.AlignToNext(quaseMeiaNoite, TimeSpan.FromMinutes(10));

        Assert.Equal(
            DateTimeOffset.Parse("2026-07-26T00:00:00Z", System.Globalization.CultureInfo.InvariantCulture),
            proximo);
    }

    [Fact]
    public void AlignToNext_MesmaCadenciaProduzOMesmoCicloParaTodosOsSegmentos()
    {
        // O cycle_id é a chave que junta os segmentos de uma camada. Se dois segmentos da mesma
        // camada caírem em ciclos diferentes, a comparação entre vias no ETL quebra.
        var cadencia = TimeSpan.FromMinutes(10);
        var t1 = DateTimeOffset.Parse("2026-07-25T03:41:03Z", System.Globalization.CultureInfo.InvariantCulture);
        var t2 = DateTimeOffset.Parse("2026-07-25T03:41:59Z", System.Globalization.CultureInfo.InvariantCulture);

        Assert.Equal(
            CycleSchedule.AlignToNext(t1, cadencia),
            CycleSchedule.AlignToNext(t2, cadencia));
    }
}
