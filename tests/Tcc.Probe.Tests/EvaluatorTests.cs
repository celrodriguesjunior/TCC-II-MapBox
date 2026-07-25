using Tcc.Domain;
using Tcc.Probe;
using Xunit;

namespace Tcc.Probe.Tests;

public class EvaluatorTests
{
    private static readonly GeoPoint[] RuaPedida =
    [
        new(-27.1223, -48.9551),
        new(-27.1223, -48.9507),
    ];

    // Via paralela ~330 m ao norte — representa a avenida vizinha em que a TomTom pode encaixar.
    private static readonly GeoPoint[] RuaVizinha =
    [
        new(-27.1193, -48.9551),
        new(-27.1193, -48.9507),
    ];

    private static readonly Candidate Candidato = new(
        Id: "rua-pedida-1-000",
        StreetName: "Rua Pedida",
        Highway: "residential",
        OsmWayId: 1,
        QueryPoint: new GeoPoint(-27.1223, -48.9529),
        OsmGeometry: RuaPedida,
        StreetGeometries: [RuaPedida]);

    private static readonly ProbeOptions Options = new();

    private static FlowSegment Segmento(IReadOnlyList<GeoPoint> geometria, double confianca = 0.9) =>
        new("FRC4", 30, 40, 60, 45, confianca, false, geometria);

    [Theory]
    [InlineData(401)]
    [InlineData(403)]
    [InlineData(429)]
    [InlineData(500)]
    [InlineData(503)]
    public void FalhaDeInfraestrutura_NaoViraSemCobertura(int status)
    {
        // Regressão: uma versão anterior classificava 401 como SemCobertura, produzindo um
        // relatório inteiro de falsas lacunas. Erro de requisição nunca é ausência de dado.
        var resposta = new FlowResponse(status, null, null, $"HTTP {status}");

        var resultado = Evaluator.Evaluate(Candidato, resposta, Options);

        Assert.Equal(Verdict.Erro, resultado.Verdict);
    }

    [Theory]
    [InlineData(401, true)]
    [InlineData(403, true)]
    [InlineData(429, true)]
    [InlineData(500, false)]
    [InlineData(200, false)]
    public void IsFatal_MarcaApenasCredencialECota(int status, bool esperado)
    {
        Assert.Equal(esperado, new FlowResponse(status, null, null, null).IsFatal);
    }

    [Fact]
    public void RespostaSemSegmento_ViraSemCobertura()
    {
        var resposta = new FlowResponse(200, "{}", null, null);

        var resultado = Evaluator.Evaluate(Candidato, resposta, Options);

        Assert.Equal(Verdict.SemCobertura, resultado.Verdict);
    }

    [Fact]
    public void Http404_ViraSemCobertura()
    {
        var resposta = new FlowResponse(404, null, null, "HTTP 404");

        var resultado = Evaluator.Evaluate(Candidato, resposta, Options);

        Assert.Equal(Verdict.SemCobertura, resultado.Verdict);
    }

    [Fact]
    public void GeometriaSobreAViaPedida_EhAceita()
    {
        var resposta = new FlowResponse(200, null, Segmento(RuaPedida), null);

        var resultado = Evaluator.Evaluate(Candidato, resposta, Options);

        Assert.Equal(Verdict.Aceito, resultado.Verdict);
        Assert.NotNull(resultado.FractionWithinTolerance);
        Assert.Equal(1.0, resultado.FractionWithinTolerance!.Value, 3);
    }

    [Fact]
    public void GeometriaSobreViaVizinha_EhRejeitada()
    {
        // O caso perigoso: HTTP 200, velocidade plausível, rua errada.
        var resposta = new FlowResponse(200, null, Segmento(RuaVizinha), null);

        var resultado = Evaluator.Evaluate(Candidato, resposta, Options);

        Assert.Equal(Verdict.GeometriaDivergente, resultado.Verdict);
    }

    [Fact]
    public void SegmentoLongoQueExtrapolaOTrecho_EhAceito()
    {
        // Regressão: a TomTom devolve o segmento inteiro em que o ponto cai, tipicamente bem
        // mais longo que o way do OSM. A versão anterior comparava a extensão toda contra um
        // único way e reprovava 97% dos pontos — inclusive casamentos com 0,8 m de precisão.
        GeoPoint[] segmentoLongo =
        [
            new(-27.1223, -48.9700),   // muito antes do trecho pedido
            new(-27.1223, -48.9551),
            new(-27.1223, -48.9529),   // sobre o ponto consultado
            new(-27.1223, -48.9507),
            new(-27.1223, -48.9350),   // muito depois
        ];

        var resposta = new FlowResponse(200, null, Segmento(segmentoLongo), null);

        var resultado = Evaluator.Evaluate(Candidato, resposta, Options);

        Assert.Equal(Verdict.Aceito, resultado.Verdict);
    }

    [Fact]
    public void RuaPartidaEmVariosTrechos_ComparaContraARuaInteira()
    {
        // No OSM uma via costuma vir partida. O casamento tem que ser julgado contra a rua toda,
        // não contra o fragmento de onde o ponto foi amostrado.
        GeoPoint[] trechoA = [new(-27.1223, -48.9551), new(-27.1223, -48.9529)];
        GeoPoint[] trechoB = [new(-27.1223, -48.9529), new(-27.1223, -48.9507)];

        var candidato = Candidato with
        {
            OsmGeometry = trechoA,
            StreetGeometries = [trechoA, trechoB],
        };

        var resposta = new FlowResponse(200, null, Segmento(RuaPedida), null);

        var resultado = Evaluator.Evaluate(candidato, resposta, Options);

        Assert.Equal(Verdict.Aceito, resultado.Verdict);
    }

    [Fact]
    public void SegmentoLongeDoPontoConsultado_EhRejeitado()
    {
        var resposta = new FlowResponse(200, null, Segmento(RuaVizinha), null);

        var resultado = Evaluator.Evaluate(Candidato, resposta, Options);

        Assert.Equal(Verdict.GeometriaDivergente, resultado.Verdict);
        Assert.True(resultado.QueryPointToMatchedMeters > Options.MaxQueryToMatchedMeters);
    }

    [Fact]
    public void ViaTransversalNoCruzamento_EhRejeitada()
    {
        // Perto de cruzamento a TomTom pode encaixar na via transversal: passa exatamente sobre
        // o ponto, mas segue em outra direção. É o motivo do recuo das extremidades na amostragem.
        GeoPoint[] transversal =
        [
            new(-27.1253, -48.9529),
            new(-27.1223, -48.9529),   // cruza sobre o ponto consultado
            new(-27.1193, -48.9529),
        ];

        var resposta = new FlowResponse(200, null, Segmento(transversal), null);

        var resultado = Evaluator.Evaluate(Candidato, resposta, Options);

        Assert.Equal(Verdict.GeometriaDivergente, resultado.Verdict);
    }

    [Fact]
    public void GeometriaCompativelMasConfiancaBaixa_EhSinalizada()
    {
        var resposta = new FlowResponse(200, null, Segmento(RuaPedida, confianca: 0.2), null);

        var resultado = Evaluator.Evaluate(Candidato, resposta, Options);

        Assert.Equal(Verdict.ConfiancaBaixa, resultado.Verdict);
    }

    [Fact]
    public void SegmentoSemGeometria_ViraSemCobertura()
    {
        var resposta = new FlowResponse(200, null, Segmento([]), null);

        var resultado = Evaluator.Evaluate(Candidato, resposta, Options);

        Assert.Equal(Verdict.SemCobertura, resultado.Verdict);
    }

    [Fact]
    public void Ratio_EhCalculadoDasVelocidades()
    {
        var resposta = new FlowResponse(200, null, Segmento(RuaPedida), null);

        var resultado = Evaluator.Evaluate(Candidato, resposta, Options);

        Assert.Equal(0.75, resultado.Ratio!.Value, 4);
    }

    [Fact]
    public void Ratio_ComFluxoLivreZero_EhNuloEmVezDeZero()
    {
        // Ausência é null, nunca um número sentinela — a regra que corrige o defeito de 2022,
        // em que "sem dado" era codificado como ratio = 0 e colidia com a faixa de congestionamento.
        var segmento = new FlowSegment("FRC4", 0, 0, 0, 0, 0.9, false, RuaPedida);
        var resposta = new FlowResponse(200, null, segmento, null);

        var resultado = Evaluator.Evaluate(Candidato, resposta, Options);

        Assert.Null(resultado.Ratio);
    }

    [Fact]
    public void BuildCandidates_RespeitaOTetoPorRua()
    {
        var ways = new[]
        {
            new OsmWay(1, "Rua Longa", "tertiary", false, RuaPedida),
            new OsmWay(2, "Rua Longa", "tertiary", false, RuaVizinha),
        };

        var candidatos = Evaluator.BuildCandidates(ways, Options with { MaxPointsPerStreet = 2, SpacingMeters = 50 });

        Assert.Equal(2, candidatos.Count);
        Assert.All(candidatos, c => Assert.Equal("Rua Longa", c.StreetName));
    }

    [Fact]
    public void BuildCandidates_GeraIdentificadoresUnicos()
    {
        var ways = new[]
        {
            new OsmWay(1, "Rua Ângelo Dias", "tertiary", false, RuaPedida),
            new OsmWay(2, "Rua São Paulo", "residential", false, RuaVizinha),
        };

        var candidatos = Evaluator.BuildCandidates(ways, Options);

        Assert.Equal(candidatos.Count, candidatos.Select(c => c.Id).Distinct(StringComparer.Ordinal).Count());
        Assert.All(candidatos, c => Assert.DoesNotContain(' ', c.Id));
    }
}
