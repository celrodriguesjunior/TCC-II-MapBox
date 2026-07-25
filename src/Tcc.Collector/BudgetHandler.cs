using System.Threading.RateLimiting;

namespace Tcc.Collector;

/// <summary>
/// Lançada quando o teto diário foi atingido. Interrompe o ciclo em vez de continuar tentando.
/// </summary>
public sealed class BudgetExhaustedException() : Exception("Orçamento diário de requisições esgotado.");

/// <summary>
/// Debita o orçamento e aplica o limite de taxa a cada requisição de saída.
///
/// Fica num <see cref="DelegatingHandler"/> de propósito: assim a contabilidade envolve todas as
/// requisições que realmente saem, **inclusive as retentativas** geradas pela camada de
/// resiliência. Contar no serviço, antes do retry, subestimaria o consumo exatamente quando a API
/// está instável e mais retentativas acontecem.
/// </summary>
public sealed class BudgetHandler(IRequestBudget budget, RateLimiter limiter) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (!budget.TryConsume())
        {
            throw new BudgetExhaustedException();
        }

        using var lease = await limiter.AcquireAsync(1, cancellationToken).ConfigureAwait(false);

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
