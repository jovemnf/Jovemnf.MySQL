using System;
using System.Reflection;
using System.Threading.Tasks;
using Jovemnf.MySQL.Builder;

namespace Jovemnf.MySQL;

public partial class MySQL
{
    public async Task<long> ExecuteInsertAsync(bool lastId = true)
    {
        EnsureCommandInitialized();
        await _cmd.ExecuteNonQueryAsync();
        if (lastId)
        {
            return await LastIdAsyncLong();
        }

        return 0;
    }

    public async Task<T> ExecuteInsertAsync<T>(bool lastId = true) where T : new()
    {
        EnsureCommandInitialized();
        await _cmd.ExecuteNonQueryAsync();
        if (lastId)
        {
            return await LastIdAsync<T>();
        }

        return new T();
    }

    public async Task<long> ExecuteInsertAsync(InsertQueryBuilder builder, bool lastId = true)
    {
        var (_, command) = builder.Build();
        AttachCommand(command);

        await this._cmd.ExecuteNonQueryAsync();

        if (lastId)
            return await LastIdAsyncLong();

        return 0;
    }

    public async Task<int> ExecuteInsertBatchAsync(InsertBatchQueryBuilder builder)
    {
        var (_, command) = builder.Build();
        AttachCommand(command);

        return await this._cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Executa o INSERT e retorna a linha inserida mapeada para T (SELECT pela chave id).
    /// </summary>
    public async Task<T> ExecuteInsertAsync<T>(InsertQueryBuilder builder) where T : new()
    {
        var lastId = await ExecuteInsertAsync(builder, lastId: true);
        if (lastId <= 0)
            return default;

        var (_, cmdSel) = builder.BuildSelectById(lastId);
        AttachCommand(cmdSel, trackAsCurrent: false);

        await using var reader = await cmdSel.ExecuteReaderAsync();
        await using var mysqlReader = new MySQLReader(reader);
        var list = await mysqlReader.ToModelListAsync<T>();
        return list.Count > 0 ? list[0] : default;
    }

    public async Task<T> ExecuteInsertAsync<T>(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var builder = new InsertQueryBuilder<T>();
        builder.ValuesFrom(entity);

        long lastId = await ExecuteInsertAsync((InsertQueryBuilder)builder, true);

        // Try to set the ID back to the entity
        object boxedEntity = entity;
        try
        {
            var idProp = typeof(T).GetProperty("Id", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (idProp != null && idProp.CanWrite)
            {
                idProp.SetValue(boxedEntity, Convert.ChangeType(lastId, idProp.PropertyType));
            }
        }
        catch
        {
            /* Ignore if ID cannot be set */
        }

        return (T)boxedEntity;
    }

    public long ExecuteInsertSync(bool lastID = true)
    {
        EnsureCommandInitialized();
        _cmd.ExecuteNonQuery();
        return lastID ? LastIdSync() : 0;
    }

    public long ExecuteInsertSync(InsertQueryBuilder builder, bool lastID = true)
    {
        var (_, command) = builder.Build();
        AttachCommand(command);

        this._cmd.ExecuteNonQuery();

        if (lastID)
            return LastIdSync();

        return 0;
    }

    public int ExecuteInsertBatchSync(InsertBatchQueryBuilder builder)
    {
        var (_, command) = builder.Build();
        AttachCommand(command);

        return this._cmd.ExecuteNonQuery();
    }

    private async Task<int> LastIdAsync()
    {
        EnsureCommandInitialized();
        this._cmd.CommandText = "SELECT LAST_INSERT_ID()";
        var result = await this._cmd.ExecuteScalarAsync();
        return result != null ? Convert.ToInt32(result) : 0;
    }

    private async Task<T> LastIdAsync<T>()
    {
        EnsureCommandInitialized();
        _cmd.CommandText = "SELECT LAST_INSERT_ID()";
        var result = await this._cmd.ExecuteScalarAsync();
        return result != null ? (T)result : default(T);
    }

    private async Task<long> LastIdAsyncLong()
    {
        EnsureCommandInitialized();
        _cmd.CommandText = "SELECT LAST_INSERT_ID()";
        object result = await _cmd.ExecuteScalarAsync();
        return result != null ? Convert.ToInt64(result) : 0;
    }

    private int LastIdSync()
    {
        EnsureCommandInitialized();
        _cmd.CommandText = "SELECT LAST_INSERT_ID()";
        var result = _cmd.ExecuteScalar();
        return result != null ? Convert.ToInt32(result) : 0;
    }
}
