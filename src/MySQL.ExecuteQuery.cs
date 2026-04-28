using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Jovemnf.MySQL.Builder;

namespace Jovemnf.MySQL;

public partial class MySQL
{
    public MySqlReader ExecuteQuerySync()
    {
        EnsureCommandInitialized();
        return new MySqlReader(this._cmd!.ExecuteReader());
    }

    public MySqlReader ExecuteQuerySync(SelectQueryBuilder builder)
    {
        var (_, command) = builder.Build();
        AttachCommand(command);
        return ExecuteQuerySync();
    }

    public async Task<MySqlReader> ExecuteQueryAsync()
    {
        EnsureCommandInitialized();
        return new MySqlReader(await _cmd!.ExecuteReaderAsync());
    }

    public async Task<MySqlReader> ExecuteQueryAsync(SelectQueryBuilder builder)
    {
        var (_, command) = builder.Build();
        AttachCommand(command);
        return await ExecuteQueryAsync();
    }

    /// <summary>
    /// Executa o SELECT e retorna todas as linhas mapeadas para o tipo T.
    /// </summary>
    public async Task<List<T>> ExecuteQueryAsync<T>(SelectQueryBuilder builder)
    {
        await using var reader = await ExecuteQueryAsync(builder);
        return await reader.ToModelListAsync<T>();
    }

    /// <summary>
    /// Executa o SELECT e retorna as linhas mapeadas para o tipo <typeparamref name="T"/>
    /// como uma sequência assíncrona (streaming), sem carregar tudo em memória.
    /// </summary>
    /// <remarks>
    /// Use esta sobrecarga para resultados grandes (milhares ou milhões de linhas): cada item é
    /// yieldado conforme chega do cursor do MySQL. O reader é fechado automaticamente quando o
    /// <c>await foreach</c> termina (ou é interrompido por <c>break</c>/exceção).
    /// </remarks>
    /// <example>
    /// <code>
    /// await foreach (var veiculo in mysql.ExecuteQueryStreamAsync&lt;Veiculo&gt;(builder, ct))
    /// {
    ///     Process(veiculo);
    /// }
    /// </code>
    /// </example>
    public async IAsyncEnumerable<T> ExecuteQueryStreamAsync<T>(
        SelectQueryBuilder builder,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await using var reader = await ExecuteQueryAsync(builder, cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            yield return reader.ToModel<T>();
        }
    }

    public long ExecuteCountSync(SelectQueryBuilder builder)
    {
        var (_, command) = builder.Build();
        AttachCommand(command);
        var result = this._cmd!.ExecuteScalar();
        return Convert.ToInt64(result);
    }

    public async Task<long> ExecuteCountAsync(SelectQueryBuilder builder)
    {
        var (_, command) = builder.Build();
        AttachCommand(command);
        var result = await _cmd!.ExecuteScalarAsync();
        return Convert.ToInt64(result);
    }

    public bool ExecuteExistsSync(SelectQueryBuilder builder)
    {
        var (_, command) = builder.BuildExists();
        AttachCommand(command);
        var result = _cmd!.ExecuteScalar();
        return result != null && result != DBNull.Value && Convert.ToInt64(result) > 0;
    }

    public async Task<bool> ExecuteExistsAsync(SelectQueryBuilder builder)
    {
        var (_, command) = builder.BuildExists();
        AttachCommand(command);
        var result = await _cmd!.ExecuteScalarAsync();
        return result != null && result != DBNull.Value && Convert.ToInt64(result) > 0;
    }
}
