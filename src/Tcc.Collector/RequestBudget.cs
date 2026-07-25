using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Tcc.Collector;

/// <summary>
/// Contador de chamadas do dia UTC corrente, persistido em disco.
///
/// Persistir é essencial: sem isso, um reinício zera o contador e a coleta estoura o teto
/// silenciosamente — que é como uma cota some sem ninguém perceber. O consumo é registrado no
/// <see cref="BudgetHandler"/>, de modo que **retentativas também gastam orçamento**; contar só
/// as tentativas bem-sucedidas subestima o consumo real justamente quando a API está instável.
/// </summary>
public interface IRequestBudget
{
    /// <summary>Reserva uma chamada. Falso quando o teto do dia foi atingido.</summary>
    bool TryConsume();

    int Remaining { get; }

    int UsedToday { get; }
}

public sealed class FileRequestBudget : IRequestBudget, IDisposable
{
    private readonly record struct State(string DayUtc, int Used);

    private readonly string _path;
    private readonly int _limit;
    private readonly ILogger<FileRequestBudget> _logger;
    private readonly Lock _gate = new();

    private string _dayUtc;
    private int _used;
    private bool _warnedExhausted;

    public FileRequestBudget(IOptions<CollectorOptions> options, ILogger<FileRequestBudget> logger)
    {
        var value = options.Value;
        _limit = value.DailyRequestBudget;
        _logger = logger;
        _path = Path.Combine(value.OutputDirectory, "budget.json");
        _dayUtc = Today();

        Directory.CreateDirectory(value.OutputDirectory);
        Load();
    }

    public int Remaining
    {
        get
        {
            lock (_gate)
            {
                RollOverIfNeeded();
                return Math.Max(0, _limit - _used);
            }
        }
    }

    public int UsedToday
    {
        get
        {
            lock (_gate)
            {
                RollOverIfNeeded();
                return _used;
            }
        }
    }

    public bool TryConsume()
    {
        lock (_gate)
        {
            RollOverIfNeeded();

            if (_used >= _limit)
            {
                if (!_warnedExhausted)
                {
                    _logger.LogError(
                        "Orçamento diário esgotado: {Used}/{Limit} chamadas em {Day}. " +
                        "Nenhuma chamada adicional até a virada do dia UTC.",
                        _used, _limit, _dayUtc);
                    _warnedExhausted = true;
                }
                return false;
            }

            _used++;
            Save();
            return true;
        }
    }

    private void RollOverIfNeeded()
    {
        var today = Today();
        if (!string.Equals(today, _dayUtc, StringComparison.Ordinal))
        {
            _logger.LogInformation(
                "Virada de dia UTC: {Previous} encerrou com {Used}/{Limit} chamadas.",
                _dayUtc, _used, _limit);

            _dayUtc = today;
            _used = 0;
            _warnedExhausted = false;
            Save();
        }
    }

    private void Load()
    {
        try
        {
            if (!File.Exists(_path))
            {
                return;
            }

            var state = JsonSerializer.Deserialize<State>(File.ReadAllText(_path));
            if (string.Equals(state.DayUtc, _dayUtc, StringComparison.Ordinal))
            {
                _used = state.Used;
                _logger.LogInformation(
                    "Orçamento retomado do disco: {Used}/{Limit} já usadas hoje.", _used, _limit);
            }
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            // Contador ilegível é tratado como zero, mas com aviso: subestimar o consumo pode
            // levar a estourar a cota, então o operador precisa saber que isso aconteceu.
            _logger.LogWarning(ex, "Não foi possível ler {Path}; recomeçando a contagem do dia em zero.", _path);
        }
    }

    private void Save()
    {
        try
        {
            File.WriteAllText(_path, JsonSerializer.Serialize(new State(_dayUtc, _used)));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, "Não foi possível gravar o contador de orçamento em {Path}.", _path);
        }
    }

    private static string Today() =>
        DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    public void Dispose()
    {
        lock (_gate)
        {
            Save();
        }
    }
}
