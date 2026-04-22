using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MySqlConnector;

namespace Jovemnf.MySQL;

public partial class MySQL
{
    /// <summary>
    /// Abre um comando SQL para execução.
    /// </summary>
    /// <param name="sql">Comando SQL a ser executado.</param>
    public void OpenCommand(string sql)
    {
        if (string.IsNullOrEmpty(sql))
            throw new ArgumentException("SQL não pode ser vazio.", nameof(sql));

        try
        {
            if (this._bdConn == null)
            {
                throw new InvalidOperationException("Conexão não foi inicializada.");
            }

            // Dispose do comando anterior se existir
            if (this._cmd != null)
            {
                this._cmd.Dispose();
            }

            this._cmd = AttachCommand(new MySqlCommand(sql, this._bdConn));
        }
        catch (Exception ex)
        {
            throw new MySQLConnectException($"Impossível estabelecer conexão com o banco de dados: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Abre um comando SQL com timeout personalizado.
    /// </summary>
    /// <param name="sql">Comando SQL a ser executado.</param>
    /// <param name="commandTimeout">Timeout do comando em segundos.</param>
    public void OpenCommand(string sql, int commandTimeout)
    {
        OpenCommand(sql);
        if (this._cmd != null)
        {
            this._cmd.CommandTimeout = commandTimeout;
        }
    }

    /*
    [Obsolete("setParameter is deprecated, please use SetParameter instead.")]
    public void setParameter(string param, object value)
    {
        this.cmd.Parameters.AddWithValue(param, value);
    }
    */

    /// <summary>
    /// Adiciona um parâmetro ao comando SQL.
    /// </summary>
    /// <param name="param">Nome do parâmetro (ex: @nome).</param>
    /// <param name="value">Valor do parâmetro.</param>
    public void SetParameter(string param, object value)
    {
        EnsureCommandInitialized();
        this._cmd.Parameters.AddWithValue(param, value ?? DBNull.Value);
    }

    /// <summary>
    /// Prepara o comando para execução no servidor (Server-side Prepared Statement).
    /// Melhora a performance em execuções repetitivas.
    /// </summary>
    public void Prepare()
    {
        EnsureCommandInitialized();
        this._cmd.Prepare();
    }

    /// <summary>
    /// Prepara o comando assincronamente para execução no servidor.
    /// </summary>
    public async Task PrepareAsync()
    {
        await PrepareAsync(CancellationToken.None);
    }

    /// <summary>
    /// Adiciona um parâmetro ao comando SQL com tipo específico.
    /// </summary>
    /// <param name="param">Nome do parâmetro (ex: @nome).</param>
    /// <param name="value">Valor do parâmetro.</param>
    /// <param name="dbType">Tipo do parâmetro no banco de dados.</param>
    public void SetParameter(string param, object value, MySqlDbType dbType)
    {
        EnsureCommandInitialized();
        var parameter = new MySqlParameter(param, dbType)
        {
            Value = value ?? DBNull.Value
        };
        this._cmd.Parameters.Add(parameter);
    }

    /// <summary>
    /// Adiciona múltiplos parâmetros ao comando de uma vez.
    /// </summary>
    /// <param name="parameters">Dicionário com nome do parâmetro como chave e valor como valor.</param>
    public void SetParameters(Dictionary<string, object> parameters)
    {
        if (parameters == null)
            throw new ArgumentNullException(nameof(parameters));

        EnsureCommandInitialized();
        foreach (var param in parameters)
        {
            _cmd.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
        }
    }

    /// <summary>
    /// Limpa todos os parâmetros do comando atual.
    /// </summary>
    public void ClearParameters()
    {
        if (_cmd != null)
        {
            _cmd.Parameters.Clear();
        }
    }

    public MySqlDataAdapter Adapter
    {
        get
        {
            return this._da;
        }
    }

    public string CommandText
    {
        get
        {
            return this._cmd?.CommandText ?? string.Empty;
        }
    }
}
