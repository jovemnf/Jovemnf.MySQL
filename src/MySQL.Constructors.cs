using System;
using Jovemnf.MySQL.Configuration;
using MySqlConnector;

namespace Jovemnf.MySQL;

public partial class MySQL
{
    /// <summary>
    /// Construtor usando objeto de configuração.
    /// </summary>
    /// <param name="config">Objeto de configuração com os dados de conexão.</param>
    public MySQL(MySQLConfiguration config)
    {
        if (config == null)
            throw new ArgumentNullException(nameof(config));

        try
        {
            if (_bdConn is not null) return;

            if (!string.IsNullOrEmpty(config.ConnectionString))
            {
                this._bdConn = new MySqlConnection(config.ConnectionString);
            }
            else
            {
                MySqlConnectionStringBuilder conn_string = new()
                {
                    Server = config.Host,
                    Port = config.Port,
                    UserID = config.Username,
                    Password = config.Password,
                    Database = config.Database,
                    CharacterSet = config.Charset,
                    SslMode = MySqlSslMode.None,
                    MaximumPoolSize = MaximumPoolSize,
                    MinimumPoolSize = MinimumPoolSize,
                    Pooling = Pooling,
                    ConnectionTimeout = ConnectionTimeout,
                    AllowUserVariables = AllowUserVariables,
                    UseCompression = UseCompression
                };

                this._bdConn = new MySqlConnection(conn_string.ToString());
            }
        }
        catch
        {
            throw;
        }
    }

    /// <summary>
    /// Construtor usando parâmetros individuais.
    /// </summary>
    /// <param name="host">Host ou endereço IP do servidor MySQL.</param>
    /// <param name="database">Nome do banco de dados.</param>
    /// <param name="username">Nome de usuário para autenticação.</param>
    /// <param name="password">Senha para autenticação.</param>
    /// <param name="port">Porta do servidor MySQL. Padrão: 3306.</param>
    /// <param name="chatset">Charset a ser usado na conexão. Padrão: utf8.</param>
    [Obsolete("Use MySQL(MySQLConfiguration config) para melhor legibilidade.")]
    public MySQL(string host, string database, string username, string password, uint port = 3306, string chatset = "utf8")
    {
        try
        {
            if (_bdConn is null)
            {
                MySqlConnectionStringBuilder conn_string = new()
                {
                    Server = host,
                    Port = port,
                    UserID = username,
                    Password = password,
                    Database = database,
                    CharacterSet = chatset,
                    SslMode = MySqlSslMode.None,
                    MaximumPoolSize = MaximumPoolSize,
                    MinimumPoolSize = MinimumPoolSize,
                    Pooling = Pooling,
                    ConnectionTimeout = ConnectionTimeout,
                    AllowUserVariables = AllowUserVariables,
                    UseCompression = UseCompression
                };

                this._bdConn = new MySqlConnection(conn_string.ToString());
            }
        }
        catch
        {
            throw;
        }
    }

    public MySQL(string stringConnect)
    {
        //this.bdDataSet = new DataSet();
        _bdConn = new MySqlConnection(stringConnect);
    }

    public MySQL()
    {
        MySqlConnectionStringBuilder connString = new()
        {
            Server = _data.HOST,
            Port = _data.Port,
            UserID = _data.UserName,
            Password = _data.PassWord,
            Database = _data.Base,
            CharacterSet = _data.Charset,
            SslMode = MySqlSslMode.None,
            Keepalive = KeepaliveInterval,
            MaximumPoolSize = MaximumPoolSize,
            MinimumPoolSize = MinimumPoolSize,
            ConnectionIdleTimeout = ConnectionIdleTimeout,
            ConnectionTimeout = ConnectionTimeout,
            ConnectionReset = ConnectionReset,
            ConnectionLifeTime = ConnectionLifeTime,
            Pooling = Pooling
        };

        _bdConn = new MySqlConnection(connString.ToString());
    }
}
