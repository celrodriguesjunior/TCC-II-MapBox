using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Tcc.Collector;

/// <summary>
/// Escrita append-only em NDJSON, um arquivo por dia UTC por fluxo.
///
/// NDJSON em vez de Parquet porque Parquet não é appendável — escrever a cada ciclo geraria
/// milhares de arquivos minúsculos. Em vez de SQLite porque um arquivo de texto é inspecionável
/// com <c>grep</c> durante uma coleta de semanas, versiona como append puro, e o DuckDB lê
/// nativamente na etapa de ETL.
///
/// Cada linha é liberada e sincronizada em disco na hora: uma queda de energia no meio da coleta
/// pode custar a última linha, nunca o arquivo.
/// </summary>
public sealed class NdjsonWriter(string directory, string stream, ILogger logger) : IAsyncDisposable
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private readonly SemaphoreSlim _gate = new(1, 1);
    private FileStream? _file;
    private string? _openDay;

    public async Task WriteAsync<T>(T record, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(record, Options);
        var bytes = Encoding.UTF8.GetBytes(json + Environment.NewLine);

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var day = DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            if (_file is null || !string.Equals(day, _openDay, StringComparison.Ordinal))
            {
                if (_file is not null)
                {
                    await _file.DisposeAsync().ConfigureAwait(false);
                    logger.LogInformation("Arquivo de {Stream} rotacionado para {Day}.", stream, day);
                }

                Directory.CreateDirectory(Path.Combine(directory, stream));
                var path = Path.Combine(directory, stream, $"{day}.ndjson");
                _file = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read);
                _openDay = day;
            }

            await _file.WriteAsync(bytes, ct).ConfigureAwait(false);
            await _file.FlushAsync(ct).ConfigureAwait(false);
            _file.Flush(flushToDisk: true);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_file is not null)
        {
            await _file.DisposeAsync().ConfigureAwait(false);
        }
        _gate.Dispose();
    }
}
