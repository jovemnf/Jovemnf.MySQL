using System;
using System.Threading.Tasks;
using MySqlConnector;

namespace Jovemnf.MySQL;

public partial class MySQL
{
    public object? ExecuteScalarSync()
    {
        try
        {
            EnsureCommandInitialized();
            return _cmd!.ExecuteScalar();
        }
        catch (Exception ex)
        {
            throw new MySQLConnectException($"Erro ao executar ExecuteScalar: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Executa um comando e retorna um único valor (ExecuteScalar).
    /// </summary>
    /// <param name="sql">Comando SQL a ser executado.</param>
    /// <returns>Primeira coluna da primeira linha do resultado ou null.</returns>
    public object? ExecuteScalarSync(string sql)
    {
        if (string.IsNullOrEmpty(sql))
            throw new ArgumentException("SQL não pode ser vazio.", nameof(sql));

        if (_bdConn == null)
            throw new InvalidOperationException("Conexão não foi inicializada.");

        try
        {
            using var scalarCommand = new MySqlCommand(sql, _bdConn);
            AttachCommand(scalarCommand, trackAsCurrent: false);
            return scalarCommand.ExecuteScalar();
        }
        catch (Exception ex)
        {
            throw new MySQLConnectException($"Erro ao executar ExecuteScalar: {ex.Message}", ex);
        }
    }

    public async Task<object?> ExecuteScalarAsync()
    {
        try
        {
            EnsureCommandInitialized();
            return await _cmd!.ExecuteScalarAsync();
        }
        catch (Exception ex)
        {
            throw new MySQLConnectException($"Erro ao executar ExecuteScalarAsync: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Executa um comando e retorna um único valor de forma assíncrona (ExecuteScalar).
    /// </summary>
    /// <param name="sql">Comando SQL a ser executado.</param>    /// <returns>Task com primeira coluna da primeira linha do resultado ou null.</returns>
    public async Task<object?> ExecuteScalarAsync(string sql)
    {
        if (string.IsNullOrEmpty(sql))
            throw new ArgumentException("SQL não pode ser vazio.", nameof(sql));

        if (_bdConn == null)
            throw new InvalidOperationException("Conexão não foi inicializada.");

        try
        {
            await using var scalarCommand = new MySqlCommand(sql, _bdConn);
            AttachCommand(scalarCommand, trackAsCurrent: false);
            return await scalarCommand.ExecuteScalarAsync();
        }
        catch (Exception ex)
        {
            throw new MySQLConnectException($"Erro ao executar ExecuteScalarAsync: {ex.Message}", ex);
        }
    }
}
