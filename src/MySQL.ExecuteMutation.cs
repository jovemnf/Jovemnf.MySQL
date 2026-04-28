using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Jovemnf.MySQL.Builder;

namespace Jovemnf.MySQL;

public partial class MySQL
{
    public int ExecuteUpdateSync()
    {
        EnsureCommandInitialized();
        return _cmd!.ExecuteNonQuery();
    }

    public int ExecuteUpdateSync(UpdateQueryBuilder builder)
    {
        var (_, command) = builder.Build();
        AttachCommand(command);

        return ExecuteUpdateSync();
    }

    public async Task<int> ExecuteUpdateAsync()
    {
        EnsureCommandInitialized();
        return await _cmd!.ExecuteNonQueryAsync();
    }

    public async Task<int> ExecuteUpdateAsync(UpdateQueryBuilder builder)
    {
        var (_, command) = builder.Build();
        AttachCommand(command);

        return await ExecuteUpdateAsync();
    }

    /// <summary>
    /// Executa o UPDATE e retorna a primeira linha afetada mapeada para T.
    /// Não suportado quando o builder usa All() (atualização em todas as linhas).
    /// </summary>
    public async Task<T?> ExecuteUpdateAsync<T>(UpdateQueryBuilder builder) where T : new()
    {
        var (_, cmdUp) = builder.Build();
        AttachCommand(cmdUp, trackAsCurrent: false);

        var rowsAffected = await cmdUp.ExecuteNonQueryAsync();
        if (rowsAffected == 0)
            return default;

        var (_, cmdSel) = builder.BuildSelect();
        AttachCommand(cmdSel, trackAsCurrent: false);

        await using var reader = await cmdSel.ExecuteReaderAsync();
        await using var mysqlReader = new MySqlReader(reader);
        var list = await mysqlReader.ToModelListAsync<T>();
        return list.Count > 0 ? list[0] : default;
    }

    public int ExecuteDeleteSync(DeleteQueryBuilder builder)
    {
        var (_, command) = builder.Build();
        AttachCommand(command);

        return _cmd!.ExecuteNonQuery();
    }

    public async Task<int> ExecuteDeleteAsync(DeleteQueryBuilder builder)
    {
        var (_, command) = builder.Build();
        AttachCommand(command);

        return await _cmd!.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Executa SELECT das linhas que atendem ao WHERE, depois DELETE, e retorna as entidades mapeadas para T.
    /// </summary>
    public async Task<List<T>> ExecuteDeleteAsync<T>(DeleteQueryBuilder builder) where T : new()
    {
        var (_, cmdSel) = builder.BuildSelect();
        AttachCommand(cmdSel, trackAsCurrent: false);

        await using var reader = await cmdSel.ExecuteReaderAsync();
        using (var mysqlReader = new MySqlReader(reader))
        {
            var list = await mysqlReader.ToModelListAsync<T>();

            var (_, cmdDel) = builder.Build();
            AttachCommand(cmdDel, trackAsCurrent: false);
            await cmdDel.ExecuteNonQueryAsync();

            return list;
        }
    }

    /// <summary>
    /// Executa múltiplos comandos SQL em uma única transação.
    /// </summary>
    /// <param name="sqlCommands">Lista de comandos SQL a serem executados.</param>
    /// <returns>Número total de linhas afetadas.</returns>
    public int ExecuteBatchSync(params string[] sqlCommands)
    {
        if (sqlCommands == null || sqlCommands.Length == 0)
            throw new ArgumentException("Pelo menos um comando SQL deve ser fornecido.", nameof(sqlCommands));

        if (_bdConn == null)
            throw new InvalidOperationException("Conexão não foi inicializada.");

        bool wasTransactionActive = _initTrans;
        int totalRowsAffected = 0;

        try
        {
            // Inicia transação se não houver uma ativa
            if (!wasTransactionActive)
            {
                BeginSync();
            }

            foreach (var sql in sqlCommands)
            {
                if (string.IsNullOrWhiteSpace(sql))
                    continue;

                OpenCommand(sql);
                totalRowsAffected += ExecuteUpdateSync();
            }

            // Commit apenas se criamos a transação
            if (!wasTransactionActive)
            {
                CommitSync();
            }

            return totalRowsAffected;
        }
        catch
        {
            // Rollback apenas se criamos a transação
            if (!wasTransactionActive)
            {
                RollbackSync();
            }

            throw;
        }
    }

    /// <summary>
    /// Executa múltiplos comandos SQL em uma única transação de forma assíncrona.
    /// </summary>
    /// <param name="sqlCommands">Lista de comandos SQL a serem executados.</param>
    /// <returns>Task com número total de linhas afetadas.</returns>
    public async Task<int> ExecuteBatchAsync(params string[] sqlCommands)
    {
        if (sqlCommands == null || sqlCommands.Length == 0)
            throw new ArgumentException("Pelo menos um comando SQL deve ser fornecido.", nameof(sqlCommands));

        if (_bdConn == null)
            throw new InvalidOperationException("Conexão não foi inicializada.");

        bool wasTransactionActive = _initTrans;
        int totalRowsAffected = 0;

        try
        {
            // Inicia transação se não houver uma ativa
            if (!wasTransactionActive)
            {
                await BeginAsync();
            }

            foreach (var sql in sqlCommands)
            {
                if (string.IsNullOrWhiteSpace(sql))
                    continue;

                OpenCommand(sql);
                totalRowsAffected += await ExecuteUpdateAsync();
            }

            // Commit apenas se criamos a transação
            if (!wasTransactionActive)
            {
                await CommitAsync();
            }

            return totalRowsAffected;
        }
        catch
        {
            // Rollback apenas se criamos a transação
            if (!wasTransactionActive)
            {
                await RollbackAsync();
            }

            throw;
        }
    }
}
