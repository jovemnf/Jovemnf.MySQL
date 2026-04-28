using System;
using Jovemnf.MySQL.Configuration;
using MySqlConnector;

namespace Jovemnf.MySQL;

public partial class MySQL
{
    /// <summary>
    /// Gerenciador global de Shards. 
    /// Permite configurar os shards no início da aplicação (Startup) e depois acessá-los em qualquer lugar.
    /// </summary>
    public static MySQLShardConfiguration GlobalShards { get; } = new MySQLShardConfiguration();

    /// <summary>
    /// Cria uma instância de MySQL usando uma Tag configurada no GlobalShards.
    /// </summary>
    /// <param name="tag">A tag do shard. Se nulo, usará o shard padrão.</param>
    /// <returns>Uma nova instância de MySQL.</returns>
    public static MySQL FromShard(object? tag = null)
    {
        return new MySQL(GlobalShards, tag);
    }

    /// <summary>
    /// Construtor usando o gerenciador de shards e uma tag específica.
    /// </summary>
    /// <param name="shardConfig">Gerenciador de configurações de shards.</param>
    /// <param name="tag">Tag do shard desejado. Pode ser string, int, etc. Se nulo ou vazio, usa a configuração padrão.</param>
    public MySQL(MySQLShardConfiguration shardConfig, object? tag = null) 
        : this(string.IsNullOrWhiteSpace(tag?.ToString()) ? shardConfig.GetDefaultShard() : shardConfig.GetShard(tag))
    {
    }

    /// <summary>
    /// Construtor usando objeto de configuração.
    /// </summary>
    /// <param name="config">Objeto de configuração com os dados de conexão.</param>
    public MySQL(MySQLConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(config);

        if (_bdConn is not null) return;

        if (string.IsNullOrEmpty(config.ConnectionString))
        {
            var pool = config.Pool;

            MySqlConnectionStringBuilder connString = new()
            {
                Server = config.Host,
                Port = config.Port,
                UserID = config.Username,
                Password = config.Password,
                Database = config.Database,
                CharacterSet = config.Charset,
                SslMode = MySqlSslMode.None,
                MaximumPoolSize = pool?.MaxPoolSize ?? MaximumPoolSize,
                MinimumPoolSize = pool?.MinPoolSize ?? MinimumPoolSize,
                Pooling = Pooling,
                ConnectionTimeout = pool?.ConnectionTimeout ?? ConnectionTimeout,
                AllowUserVariables = AllowUserVariables,
                UseCompression = UseCompression,
                ConnectionIdleTimeout = pool?.IdleTimeout ?? ConnectionIdleTimeout,
                ConnectionReset = pool?.ConnectionReset ?? ConnectionReset,
                ConnectionLifeTime = pool?.ConnectionLifeTime ?? ConnectionLifeTime,
                Keepalive = pool?.KeepaliveInterval ?? KeepaliveInterval
            };

            // Cache: evita reconstruir o builder nas próximas instâncias deste shard
            config.ConnectionString = connString.ToString();
        }

        _bdConn = new MySqlConnection(config.ConnectionString);
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
        if (_bdConn is not null) return;
        MySqlConnectionStringBuilder connString = new()
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

        _bdConn = new MySqlConnection(connString.ToString());
    }

    public MySQL(string stringConnect)
    {
        //this.bdDataSet = new DataSet();
        _bdConn = new MySqlConnection(stringConnect);
    }

    public MySQL()
    {
        // Se existem shards configurados, usa o shard default
        if (GlobalShards.HasShards)
        {
            var defaultConfig = GlobalShards.GetDefaultShard();

            if (string.IsNullOrEmpty(defaultConfig.ConnectionString))
            {
                var pool = defaultConfig.Pool;

                MySqlConnectionStringBuilder connString = new()
                {
                    Server = defaultConfig.Host,
                    Port = defaultConfig.Port,
                    UserID = defaultConfig.Username,
                    Password = defaultConfig.Password,
                    Database = defaultConfig.Database,
                    CharacterSet = defaultConfig.Charset,
                    SslMode = MySqlSslMode.None,
                    MaximumPoolSize = pool?.MaxPoolSize ?? MaximumPoolSize,
                    MinimumPoolSize = pool?.MinPoolSize ?? MinimumPoolSize,
                    Pooling = Pooling,
                    ConnectionTimeout = pool?.ConnectionTimeout ?? ConnectionTimeout,
                    AllowUserVariables = AllowUserVariables,
                    UseCompression = UseCompression,
                    ConnectionIdleTimeout = pool?.IdleTimeout ?? ConnectionIdleTimeout,
                    ConnectionReset = pool?.ConnectionReset ?? ConnectionReset,
                    ConnectionLifeTime = pool?.ConnectionLifeTime ?? ConnectionLifeTime,
                    Keepalive = pool?.KeepaliveInterval ?? KeepaliveInterval
                };

                // Cache: evita reconstruir o builder nas próximas instâncias deste shard
                defaultConfig.ConnectionString = connString.ToString();
            }

            _bdConn = new MySqlConnection(defaultConfig.ConnectionString);

            return;
        }

        // Fallback: usa a configuração legada via Init()
        MySqlConnectionStringBuilder legacyConnString = new()
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

        _bdConn = new MySqlConnection(legacyConnString.ToString());
    }
}
