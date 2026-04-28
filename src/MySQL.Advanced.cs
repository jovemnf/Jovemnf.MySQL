using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jovemnf.MySQL.Builder;
using Jovemnf.MySQL.Configuration;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace Jovemnf.MySQL;

public partial class MySQL
{
    public static MySQLOptions DefaultOptions { get; } = new();

    public MySQLOptions Options { get; private set; } = DefaultOptions;

    public MySQL(MySQLConfiguration config, MySQLOptions options) : this(config)
    {
        Options = options ?? DefaultOptions;
    }

#pragma warning disable CS0618
    public MySQL(string host, string database, string username, string password, MySQLOptions options, uint port = 3306, string chatset = "utf8")
        : this(host, database, username, password, port, chatset)
    {
        Options = options ?? DefaultOptions;
    }
#pragma warning restore CS0618

    public MySQL(string stringConnect, MySQLOptions options) : this(stringConnect)
    {
        Options = options ?? DefaultOptions;
    }

    public MySQL(MySQLOptions options) : this()
    {
        Options = options ?? DefaultOptions;
    }

    public void ConfigureOptions(MySQLOptions options)
    {
        Options = options ?? DefaultOptions;
    }

    public async Task<long> ExecuteInsertAsync(InsertQueryBuilder builder, bool lastId = true, CancellationToken cancellationToken = default)
    {
        var (_, command) = builder.Build();
        AttachCommand(command);

        var rowsAffected = await ExecuteNonQueryLoggedAsync("Insert", _cmd, cancellationToken);
        return lastId ? await LastIdAsyncLong(cancellationToken) : rowsAffected;
    }

    public async Task<int> ExecuteInsertBatchAsync(InsertBatchQueryBuilder builder, CancellationToken cancellationToken = default)
    {
        var (_, command) = builder.Build();
        AttachCommand(command);

        return await ExecuteNonQueryLoggedAsync("InsertBatch", _cmd, cancellationToken);
    }

    public async Task<T?> ExecuteInsertAsync<T>(InsertQueryBuilder builder, CancellationToken cancellationToken) where T : new()
    {
        var lastId = await ExecuteInsertAsync(builder, lastId: true, cancellationToken);
        if (lastId <= 0)
            return default;

        var (_, cmdSel) = builder.BuildSelectById(lastId);
        AttachCommand(cmdSel, trackAsCurrent: false);

        await using var reader = await ExecuteReaderLoggedAsync("InsertSelect", cmdSel, cancellationToken);
        await using var mysqlReader = new MySqlReader(reader);
        var list = await mysqlReader.ToModelListAsync<T>(cancellationToken);
        return list.Count > 0 ? list[0] : default;
    }

    public async Task<MySqlReader> ExecuteQueryAsync(CancellationToken cancellationToken = default)
    {
        EnsureCommandInitialized();
        var reader = await ExecuteReaderLoggedAsync("Query", this._cmd!, cancellationToken);
        return new MySqlReader(reader);
    }

    public async Task<MySqlReader> ExecuteQueryAsync(SelectQueryBuilder builder, CancellationToken cancellationToken = default)
    {
        var (_, command) = builder.Build();
        AttachCommand(command);
        return await ExecuteQueryAsync(cancellationToken);
    }

    public async Task<List<T>> ExecuteQueryAsync<T>(SelectQueryBuilder builder, CancellationToken cancellationToken = default)
    {
        await using var reader = await ExecuteQueryAsync(builder, cancellationToken);
        return await reader.ToModelListAsync<T>(cancellationToken);
    }

    public async Task<List<TResult>> ExecuteQueryAsync<TFirst, TSecond, TResult>(
        SelectQueryBuilder builder,
        Func<TFirst, TSecond, TResult> map,
        string splitOn = "id",
        CancellationToken cancellationToken = default)
    {
        await using var reader = await ExecuteQueryAsync(builder, cancellationToken);
        return await reader.ToMultiMapListAsync(map, splitOn, cancellationToken);
    }

    public async Task<long> ExecuteCountAsync(SelectQueryBuilder builder, CancellationToken cancellationToken = default)
    {
        var (_, command) = builder.BuildCount();
        AttachCommand(command);
        var result = await ExecuteScalarLoggedAsync("Count", this._cmd!, cancellationToken);
        return Convert.ToInt64(result);
    }

    public async Task<bool> ExecuteExistsAsync(SelectQueryBuilder builder, CancellationToken cancellationToken = default)
    {
        var (_, command) = builder.BuildExists();
        AttachCommand(command);
        var result = await ExecuteScalarLoggedAsync("Exists", _cmd!, cancellationToken);
        return result != null && result != DBNull.Value && Convert.ToInt64(result) > 0;
    }

    public async Task BeginAsync(CancellationToken cancellationToken)
    {
        if (_bdConn == null)
            throw new InvalidOperationException("Conexão não foi inicializada.");

        _initTrans = true;
        trans = await _bdConn.BeginTransactionAsync(cancellationToken);
    }

    public async Task RollbackAsync(CancellationToken cancellationToken)
    {
        if (trans != null)
        {
            await trans.RollbackAsync(cancellationToken);
            await trans.DisposeAsync();
            trans = null;
            _initTrans = false;
        }
    }

    public async Task CommitAsync(CancellationToken cancellationToken)
    {
        if (trans != null)
        {
            await trans.CommitAsync(cancellationToken);
            await trans.DisposeAsync();
            trans = null;
            _initTrans = false;
        }
    }

    public async Task<int> ExecuteUpdateAsync(CancellationToken cancellationToken = default)
    {
        EnsureCommandInitialized();
        return await ExecuteNonQueryLoggedAsync("Update", this._cmd, cancellationToken);
    }

    public async Task<int> ExecuteUpdateAsync(UpdateQueryBuilder builder, CancellationToken cancellationToken = default)
    {
        var (_, command) = builder.Build();
        AttachCommand(command);

        return await ExecuteUpdateAsync(cancellationToken);
    }

    public async Task<T?> ExecuteUpdateAsync<T>(UpdateQueryBuilder builder, CancellationToken cancellationToken) where T : new()
    {
        var (_, cmdUp) = builder.Build();
        AttachCommand(cmdUp, trackAsCurrent: false);

        var rowsAffected = await ExecuteNonQueryLoggedAsync("Update", cmdUp, cancellationToken);
        if (rowsAffected == 0)
            return default;

        var (_, cmdSel) = builder.BuildSelect();
        AttachCommand(cmdSel, trackAsCurrent: false);

        await using var reader = await ExecuteReaderLoggedAsync("UpdateSelect", cmdSel, cancellationToken);
        await using var mysqlReader = new MySqlReader(reader);
        var list = await mysqlReader.ToModelListAsync<T>(cancellationToken);
        return list.Count > 0 ? list[0] : default;
    }

    public async Task<int> ExecuteDeleteAsync(DeleteQueryBuilder builder, CancellationToken cancellationToken = default)
    {
        var (_, command) = builder.Build();
        AttachCommand(command);

        return await ExecuteNonQueryLoggedAsync("Delete", _cmd, cancellationToken);
    }

    public async Task<List<T>> ExecuteDeleteAsync<T>(DeleteQueryBuilder builder, CancellationToken cancellationToken) where T : new()
    {
        var (_, cmdSel) = builder.BuildSelect();
        AttachCommand(cmdSel, trackAsCurrent: false);

        await using var reader = await ExecuteReaderLoggedAsync("DeleteSelect", cmdSel, cancellationToken);
        await using var mysqlReader = new MySqlReader(reader);
        var list = await mysqlReader.ToModelListAsync<T>(cancellationToken);

        var (_, cmdDel) = builder.Build();
        AttachCommand(cmdDel, trackAsCurrent: false);
        await ExecuteNonQueryLoggedAsync("Delete", cmdDel, cancellationToken);

        return list;
    }

    public async Task OpenAsync(CancellationToken cancellationToken)
    {
        if (this._bdConn != null && this._bdConn.State != ConnectionState.Open)
        {
            await this._bdConn.OpenAsync(cancellationToken);
        }
    }

    public async Task PrepareAsync(CancellationToken cancellationToken)
    {
        EnsureCommandInitialized();
        await _cmd!.PrepareAsync(cancellationToken);
    }

    public async Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken = default)
    {
        EnsureCommandInitialized();
        return await ExecuteScalarLoggedAsync("Scalar", _cmd!, cancellationToken);
    }

    public async Task<object?> ExecuteScalarAsync(string sql, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(sql))
            throw new ArgumentException("SQL não pode ser vazio.", nameof(sql));

        if (_bdConn == null)
            throw new InvalidOperationException("Conexão não foi inicializada.");

        await using var scalarCommand = new MySqlCommand(sql, _bdConn);
        AttachCommand(scalarCommand, trackAsCurrent: false);

        return await ExecuteScalarLoggedAsync("Scalar", scalarCommand, cancellationToken);
    }

    public async Task<int> ExecuteBatchAsync(CancellationToken cancellationToken = default, params string[] sqlCommands)
    {
        if (sqlCommands == null || sqlCommands.Length == 0)
            throw new ArgumentException("Pelo menos um comando SQL deve ser fornecido.", nameof(sqlCommands));

        if (_bdConn == null)
            throw new InvalidOperationException("Conexão não foi inicializada.");

        var wasTransactionActive = _initTrans;
        var totalRowsAffected = 0;

        try
        {
            if (!wasTransactionActive)
                await BeginAsync(cancellationToken);

            foreach (var sql in sqlCommands)
            {
                if (string.IsNullOrWhiteSpace(sql))
                    continue;

                OpenCommand(sql);
                totalRowsAffected += await ExecuteUpdateAsync(cancellationToken);
            }

            if (!wasTransactionActive)
                await CommitAsync(cancellationToken);

            return totalRowsAffected;
        }
        catch
        {
            if (!wasTransactionActive)
                await RollbackAsync(cancellationToken);

            throw;
        }
    }

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_bdConn == null)
                return false;

            var wasOpen = _bdConn.State == ConnectionState.Open;
            if (!wasOpen)
                await OpenAsync(cancellationToken);

            await using var connectionTestCommand = new MySqlCommand("SELECT 1", _bdConn);
            await ExecuteScalarLoggedAsync("TestConnection", connectionTestCommand, cancellationToken);

            if (!wasOpen)
                await CloseAsync();

            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task WithTransactionAsync(Func<MySQL, Task> action, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);
        await WithTransactionAsync<object>(async db =>
        {
            await action(db);
            return null!;
        }, cancellationToken);
    }

    public async Task<TResult> WithTransactionAsync<TResult>(Func<MySQL, Task<TResult>> action, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);

        var openedHere = State != ConnectionState.Open;
        var createdTransaction = !HasActiveTransaction;

        if (openedHere)
            await OpenAsync(cancellationToken);

        if (createdTransaction)
            await BeginAsync(cancellationToken);

        try
        {
            var result = await action(this);
            if (createdTransaction)
                await CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            if (createdTransaction && HasActiveTransaction)
                await RollbackAsync(cancellationToken);
            throw;
        }
        finally
        {
            if (openedHere)
                await CloseAsync();
        }
    }

    public async Task<PagedResult<T>> PaginateAsync<T>(
        SelectQueryBuilder builder,
        PageRequest request,
        CancellationToken cancellationToken = default) where T : new()
    {
        ArgumentNullException.ThrowIfNull(builder);

        var countBuilder = builder.Clone();
        var pageBuilder = builder.Clone().Limit(request.PageSize, request.Offset);

        var total = await ExecuteCountAsync(countBuilder, cancellationToken);
        var items = await ExecuteQueryAsync<T>(pageBuilder, cancellationToken);

        return new PagedResult<T>
        {
            Items = items,
            TotalItems = total,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    public async Task<int> BulkInsertAsync<T>(
        IEnumerable<T> items,
        int? chunkSize = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(items);
        return await BulkInsertInternalAsync(items, null, chunkSize, cancellationToken);
    }

    public async Task<int> BulkUpsertAsync<T>(
        IEnumerable<T> items,
        string[] updateFields,
        int? chunkSize = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(updateFields);
        return await BulkInsertInternalAsync(items, builder => builder.OnDuplicateKeyUpdate(updateFields), chunkSize, cancellationToken);
    }

    public async Task<int> BulkUpsertAllExceptAsync<T>(
        IEnumerable<T> items,
        string[] excludedFields,
        int? chunkSize = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(items);
        return await BulkInsertInternalAsync(items, builder => builder.OnDuplicateKeyUpdateAllExcept(excludedFields), chunkSize, cancellationToken);
    }

    private async Task<int> BulkInsertInternalAsync<T>(
        IEnumerable<T> items,
        Action<InsertBatchQueryBuilder<T>>? configureUpsert,
        int? chunkSize,
        CancellationToken cancellationToken)
    {
        var effectiveChunkSize = chunkSize.GetValueOrDefault(Options?.Bulk?.DefaultChunkSize ?? DefaultOptions.Bulk.DefaultChunkSize);
        if (effectiveChunkSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(chunkSize), "O tamanho do chunk deve ser maior que zero.");

        var total = 0;
        foreach (var chunk in Chunk(items, effectiveChunkSize))
        {
            var builder = InsertBatchQueryBuilder.For<T>().RowsFrom(chunk);
            configureUpsert?.Invoke(builder);
            total += await ExecuteInsertBatchAsync(builder, cancellationToken);
        }

        return total;
    }

    private static IEnumerable<List<T>> Chunk<T>(IEnumerable<T> items, int chunkSize)
    {
        var bucket = new List<T>(chunkSize);
        foreach (var item in items)
        {
            bucket.Add(item);
            if (bucket.Count == chunkSize)
            {
                yield return bucket;
                bucket = new List<T>(chunkSize);
            }
        }

        if (bucket.Count > 0)
            yield return bucket;
    }

    private async Task<int> ExecuteNonQueryLoggedAsync(string operation, MySqlCommand? command, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var rowsAffected = await command!.ExecuteNonQueryAsync(cancellationToken);
            LogSuccess(operation, command, stopwatch.Elapsed, rowsAffected);
            return rowsAffected;
        }
        catch (Exception ex)
        {
            LogFailure(operation, command, stopwatch.Elapsed, ex);
            throw;
        }
    }

    private async Task<object?> ExecuteScalarLoggedAsync(string operation, MySqlCommand command, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = await command.ExecuteScalarAsync(cancellationToken);
            LogSuccess(operation, command, stopwatch.Elapsed, null);
            return result;
        }
        catch (Exception ex)
        {
            LogFailure(operation, command, stopwatch.Elapsed, ex);
            throw;
        }
    }

    private async Task<DbDataReader> ExecuteReaderLoggedAsync(string operation, MySqlCommand command, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var reader = await command.ExecuteReaderAsync(cancellationToken);
            LogSuccess(operation, command, stopwatch.Elapsed, null);
            return reader;
        }
        catch (Exception ex)
        {
            LogFailure(operation, command, stopwatch.Elapsed, ex);
            throw;
        }
    }

    private async Task<long> LastIdAsyncLong(CancellationToken cancellationToken)
    {
        EnsureCommandInitialized();
        _cmd!.CommandText = "SELECT LAST_INSERT_ID()";
        object? result = await ExecuteScalarLoggedAsync("LastInsertId", this._cmd, cancellationToken);
        return result != null ? Convert.ToInt64(result) : 0;
    }

    private void LogSuccess(string operation, MySqlCommand command, TimeSpan elapsed, int? rowsAffected)
    {
        var logger = Options?.Logger ?? DefaultOptions.Logger;
        if (logger == null)
            return;

        logger.LogInformation(
            "MySQL {Operation} executado em {ElapsedMs} ms. Transaction={HasTransaction}, RowsAffected={RowsAffected}, Sql={Sql}, DebugSql={DebugSql}",
            operation,
            elapsed.TotalMilliseconds,
            HasActiveTransaction,
            rowsAffected,
            command.CommandText,
            MaskSql(BuildDebugSql(command)));
    }

    private void LogFailure(string operation, MySqlCommand? command, TimeSpan elapsed, Exception exception)
    {
        var logger = Options?.Logger ?? DefaultOptions.Logger;
        if (logger == null)
            return;

        logger.LogError(
            exception,
            "MySQL {Operation} falhou em {ElapsedMs} ms. Transaction={HasTransaction}, Sql={Sql}, DebugSql={DebugSql}",
            operation,
            elapsed.TotalMilliseconds,
            HasActiveTransaction,
            command!.CommandText,
            MaskSql(BuildDebugSql(command)));
    }

    private string MaskSql(string sql)
    {
        var masker = Options?.SqlMasker ?? DefaultOptions.SqlMasker;
        return masker?.Invoke(sql) ?? sql;
    }

    internal static string BuildDebugSql(MySqlCommand command)
    {
        var sql = command.CommandText;
        foreach (var parameter in command.Parameters.OrderByDescending(p => p.ParameterName.Length))
        {
            sql = sql.Replace(parameter.ParameterName, FormatValue(parameter.Value), StringComparison.Ordinal);
        }

        return sql;
    }

    private static string FormatValue(object? value)
    {
        if (value == null || value == DBNull.Value)
            return "NULL";

        return value switch
        {
            string text => $"'{text.Replace("'", "''")}'",
            bool boolean => boolean ? "1" : "0",
            DateTime dateTime => $"'{dateTime:yyyy-MM-dd HH:mm:ss.fffffff}'",
            DateTimeOffset dateTimeOffset => $"'{dateTimeOffset:yyyy-MM-dd HH:mm:ss.fffffff zzz}'",
            Guid guid => $"'{guid}'",
            byte[] bytes => $"0x{Convert.ToHexString(bytes)}",
            Enum enumValue => Convert.ToInt64(enumValue, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture),
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => $"'{value.ToString()?.Replace("'", "''")}'"
        };
    }
}
