using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Jovemnf.MySQL.Builder;

namespace Jovemnf.MySQL;

public partial class MySQL
{
    public MySQLReader ExecuteQuerySync()
    {
        EnsureCommandInitialized();
        return new MySQLReader(this._cmd.ExecuteReader());
    }

    public MySQLReader ExecuteQuerySync(SelectQueryBuilder builder)
    {
        var (_, command) = builder.Build();
        AttachCommand(command);
        return ExecuteQuerySync();
    }

    public async Task<MySQLReader> ExecuteQueryAsync()
    {
        EnsureCommandInitialized();
        return new MySQLReader(await _cmd.ExecuteReaderAsync());
    }

    public async Task<MySQLReader> ExecuteQueryAsync(SelectQueryBuilder builder)
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

    public long ExecuteCountSync(SelectQueryBuilder builder)
    {
        var (_, command) = builder.Build();
        AttachCommand(command);
        var result = this._cmd.ExecuteScalar();
        return Convert.ToInt64(result);
    }

    public async Task<long> ExecuteCountAsync(SelectQueryBuilder builder)
    {
        var (_, command) = builder.Build();
        AttachCommand(command);
        var result = await _cmd.ExecuteScalarAsync();
        return Convert.ToInt64(result);
    }

    public bool ExecuteExistsSync(SelectQueryBuilder builder)
    {
        var (_, command) = builder.BuildExists();
        AttachCommand(command);
        var result = _cmd.ExecuteScalar();
        return result != null && result != DBNull.Value && Convert.ToInt64(result) > 0;
    }

    public async Task<bool> ExecuteExistsAsync(SelectQueryBuilder builder)
    {
        var (_, command) = builder.BuildExists();
        AttachCommand(command);
        var result = await _cmd.ExecuteScalarAsync();
        return result != null && result != DBNull.Value && Convert.ToInt64(result) > 0;
    }
}
