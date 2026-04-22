using System;
using System.Collections.Generic;
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
