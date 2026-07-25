using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Tcc.Collector;
using Xunit;

namespace Tcc.Collector.Tests;

public sealed class RequestBudgetTests : IDisposable
{
    private readonly string _dir = Path.Combine(
        Path.GetTempPath(), "tcc-budget-" + Guid.NewGuid().ToString("N")[..8]);

    private FileRequestBudget Create(int limite) => new(
        Options.Create(new CollectorOptions { OutputDirectory = _dir, DailyRequestBudget = limite }),
        NullLogger<FileRequestBudget>.Instance);

    [Fact]
    public void Consome_AteOLimite_EDepoisRecusa()
    {
        using var budget = Create(3);

        Assert.True(budget.TryConsume());
        Assert.True(budget.TryConsume());
        Assert.True(budget.TryConsume());
        Assert.False(budget.TryConsume());

        Assert.Equal(3, budget.UsedToday);
        Assert.Equal(0, budget.Remaining);
    }

    [Fact]
    public void Sobrevive_AoReinicioDoProcesso()
    {
        // O ponto central de persistir em disco: sem isto, cada reinício zeraria o contador e a
        // coleta estouraria a cota sem ninguém perceber.
        using (var primeiro = Create(100))
        {
            for (var i = 0; i < 7; i++)
            {
                primeiro.TryConsume();
            }
        }

        using var segundo = Create(100);

        Assert.Equal(7, segundo.UsedToday);
        Assert.Equal(93, segundo.Remaining);
    }

    [Fact]
    public void ContadorCorrompido_RecomecaEmZero_SemDerrubarOColetor()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(Path.Combine(_dir, "budget.json"), "{ isto não é json válido");

        using var budget = Create(50);

        Assert.Equal(0, budget.UsedToday);
        Assert.True(budget.TryConsume());
    }

    [Fact]
    public void SemArquivoPrevio_ComecaZerado()
    {
        using var budget = Create(10);

        Assert.Equal(0, budget.UsedToday);
        Assert.Equal(10, budget.Remaining);
    }

    [Fact]
    public void Remaining_NuncaFicaNegativo()
    {
        using var budget = Create(1);

        budget.TryConsume();
        budget.TryConsume();
        budget.TryConsume();

        Assert.Equal(0, budget.Remaining);
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, recursive: true);
        }
    }
}

public class CollectorOptionsTests
{
    [Fact]
    public void CadenceFor_CamadaNaoConfigurada_CaiNoValorConservador()
    {
        // Camada sem cadência configurada não pode virar consulta a cada instante: o padrão
        // tem que ser o mais econômico, nunca o mais agressivo.
        var options = new CollectorOptions { CadenceMinutesByTier = [] };

        Assert.Equal(30, options.CadenceFor(Tcc.Domain.SegmentTier.Espinha));
    }

    [Fact]
    public void CadenceFor_ValorInvalido_CaiNoValorConservador()
    {
        var options = new CollectorOptions
        {
            CadenceMinutesByTier = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["Espinha"] = 0,
            },
        };

        Assert.Equal(30, options.CadenceFor(Tcc.Domain.SegmentTier.Espinha));
    }

    [Fact]
    public void CadenceFor_LeAConfiguracaoIndependenteDeCaixa()
    {
        var options = new CollectorOptions
        {
            CadenceMinutesByTier = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["espinha"] = 10,
            },
        };

        Assert.Equal(10, options.CadenceFor(Tcc.Domain.SegmentTier.Espinha));
    }
}
