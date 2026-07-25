using Tcc.Probe;
using Xunit;

namespace Tcc.Probe.Tests;

public class GeoTests
{
    // Trecho reto de ~440 m no sentido leste-oeste, na latitude de Rio Branco.
    private static readonly GeoPoint[] StraightLine =
    [
        new(-27.1223, -48.9551),
        new(-27.1223, -48.9507),
    ];

    [Fact]
    public void Distance_EntrePontosConhecidos_BateComOEsperado()
    {
        // 0.0044° de longitude na latitude -27.12 ≈ 435 m.
        var d = Geo.Distance(StraightLine[0], StraightLine[1]);
        Assert.InRange(d, 420, 450);
    }

    [Fact]
    public void Distance_MesmoPonto_EhZero()
    {
        Assert.Equal(0, Geo.Distance(StraightLine[0], StraightLine[0]), 6);
    }

    [Fact]
    public void DistanceToPolyline_PontoSobreOMeioDoSegmento_EhQuaseZero()
    {
        // Este é o caso que uma implementação vértice-a-vértice erraria: o ponto está a
        // ~220 m de ambos os vértices, mas exatamente sobre a linha.
        var meio = new GeoPoint(-27.1223, -48.9529);
        Assert.InRange(Geo.DistanceToPolyline(meio, StraightLine), 0, 1.0);
    }

    [Fact]
    public void DistanceToPolyline_PontoDeslocadoPerpendicularmente_RetornaODeslocamento()
    {
        // 0.00045° de latitude ≈ 50 m ao norte da linha.
        var deslocado = new GeoPoint(-27.12185, -48.9529);
        Assert.InRange(Geo.DistanceToPolyline(deslocado, StraightLine), 45, 55);
    }

    [Fact]
    public void DistanceToPolyline_PontoAlemDaExtremidade_UsaAExtremidade()
    {
        var alem = new GeoPoint(-27.1223, -48.9451);
        var esperado = Geo.Distance(alem, StraightLine[1]);
        Assert.InRange(Geo.DistanceToPolyline(alem, StraightLine), esperado - 2, esperado + 2);
    }

    [Fact]
    public void PointAtDistance_NoMeio_FicaSobreALinha()
    {
        var total = Geo.Length(StraightLine);
        var meio = Geo.PointAtDistance(StraightLine, total / 2);

        Assert.InRange(Geo.DistanceToPolyline(meio, StraightLine), 0, 1.0);
        Assert.InRange(Geo.Distance(StraightLine[0], meio), (total / 2) - 2, (total / 2) + 2);
    }

    [Fact]
    public void SampleAlong_ViaCurta_RetornaApenasOPontoMedio()
    {
        // 100 m de via com recuo de 40 m em cada ponta não comporta amostragem espaçada.
        GeoPoint[] curta = [new(-27.1223, -48.9551), new(-27.1223, -48.9541)];

        var pontos = Geo.SampleAlong(curta, spacingMeters: 400, edgeMarginMeters: 40, maxPoints: 4);

        Assert.Single(pontos);
        Assert.InRange(Geo.DistanceToPolyline(pontos[0], curta), 0, 1.0);
    }

    [Fact]
    public void SampleAlong_RespeitaORecuoDasExtremidades()
    {
        var pontos = Geo.SampleAlong(StraightLine, spacingMeters: 100, edgeMarginMeters: 40, maxPoints: 10);

        Assert.All(pontos, p =>
        {
            Assert.True(Geo.Distance(p, StraightLine[0]) >= 39, "ponto perto demais do início");
            Assert.True(Geo.Distance(p, StraightLine[1]) >= 39, "ponto perto demais do fim");
        });
    }

    [Fact]
    public void SampleAlong_RespeitaOTetoDePontos()
    {
        var pontos = Geo.SampleAlong(StraightLine, spacingMeters: 10, edgeMarginMeters: 40, maxPoints: 3);
        Assert.Equal(3, pontos.Count);
    }

    [Fact]
    public void SampleAlong_TodosOsPontosCaemSobreAVia()
    {
        var pontos = Geo.SampleAlong(StraightLine, spacingMeters: 100, edgeMarginMeters: 40, maxPoints: 10);

        Assert.NotEmpty(pontos);
        Assert.All(pontos, p => Assert.InRange(Geo.DistanceToPolyline(p, StraightLine), 0, 1.0));
    }
}
