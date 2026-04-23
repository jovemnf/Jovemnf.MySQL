using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Jovemnf.MySQL.Builder;

namespace Jovemnf.MySQL;

public partial class DatabaseHelper
{
    public MySQLOptions Options { get; } = MySQL.DefaultOptions;

    public DatabaseHelper(string connectionString, MySQLOptions options) : this(connectionString)
    {
        Options = options ?? MySQL.DefaultOptions;
    }

    public async Task<int> ExecuteUpdateAsync(UpdateQueryBuilder builder, CancellationToken cancellationToken = default)
    {
        await using var mysql = new MySQL(_connectionString, Options);
        await mysql.OpenAsync(cancellationToken);
        return await mysql.ExecuteUpdateAsync(builder, cancellationToken);
    }

    public async Task<int> ExecuteDeleteAsync(DeleteQueryBuilder builder, CancellationToken cancellationToken = default)
    {
        await using var mysql = new MySQL(_connectionString, Options);
        await mysql.OpenAsync(cancellationToken);
        return await mysql.ExecuteDeleteAsync(builder, cancellationToken);
    }

    public async Task<long> ExecuteInsertAsync(InsertQueryBuilder builder, bool lastID = true, CancellationToken cancellationToken = default)
    {
        await using var mysql = new MySQL(_connectionString, Options);
        await mysql.OpenAsync(cancellationToken);
        return await mysql.ExecuteInsertAsync(builder, lastID, cancellationToken);
    }

    public async Task<int> ExecuteInsertBatchAsync(InsertBatchQueryBuilder builder, CancellationToken cancellationToken = default)
    {
        await using var mysql = new MySQL(_connectionString, Options);
        await mysql.OpenAsync(cancellationToken);
        return await mysql.ExecuteInsertBatchAsync(builder, cancellationToken);
    }

    public async Task<MySQLReader> ExecuteQueryAsync(SelectQueryBuilder builder, CancellationToken cancellationToken = default)
    {
        var mysql = new MySQL(_connectionString, Options);
        await mysql.OpenAsync(cancellationToken);
        return await mysql.ExecuteQueryAsync(builder, cancellationToken);
    }

    public async Task<List<T>> ExecuteQueryAsync<T>(SelectQueryBuilder builder, CancellationToken cancellationToken = default) where T : new()
    {
        await using var mysql = new MySQL(_connectionString, Options);
        await mysql.OpenAsync(cancellationToken);
        return await mysql.ExecuteQueryAsync<T>(builder, cancellationToken);
    }

    /// <summary>
    /// Streaming do resultado do SELECT, mantendo a conexão aberta apenas enquanto o
    /// <c>await foreach</c> estiver consumindo linhas. Indicado para resultados grandes.
    /// </summary>
    /// <remarks>
    /// A conexão é aberta no início do <c>await foreach</c> e fechada automaticamente ao final
    /// (ou ao interromper com <c>break</c>/exceção). Não consuma de forma demorada dentro do loop,
    /// pois você estará segurando uma conexão do pool até terminar a enumeração.
    /// </remarks>
    public async IAsyncEnumerable<T> ExecuteQueryStreamAsync<T>(
        SelectQueryBuilder builder,
        [EnumeratorCancellation] CancellationToken cancellationToken = default) where T : new()
    {
        await using var mysql = new MySQL(_connectionString, Options);
        await mysql.OpenAsync(cancellationToken).ConfigureAwait(false);
        await foreach (var item in mysql.ExecuteQueryStreamAsync<T>(builder, cancellationToken).ConfigureAwait(false))
        {
            yield return item;
        }
    }

    public async Task<PagedResult<T>> PaginateAsync<T>(SelectQueryBuilder builder, PageRequest request, CancellationToken cancellationToken = default) where T : new()
    {
        await using var mysql = new MySQL(_connectionString, Options);
        await mysql.OpenAsync(cancellationToken);
        return await mysql.PaginateAsync<T>(builder, request, cancellationToken);
    }

    public async Task<TResult> WithTransactionAsync<TResult>(Func<MySQL, Task<TResult>> action, CancellationToken cancellationToken = default)
    {
        await using var mysql = new MySQL(_connectionString, Options);
        return await mysql.WithTransactionAsync(action, cancellationToken);
    }

    public async Task WithTransactionAsync(Func<MySQL, Task> action, CancellationToken cancellationToken = default)
    {
        await using var mysql = new MySQL(_connectionString, Options);
        await mysql.WithTransactionAsync(action, cancellationToken);
    }
}
