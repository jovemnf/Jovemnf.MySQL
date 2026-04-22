using System;
using System.Runtime.InteropServices;
using Jovemnf.MySQL.Configuration;
using MySqlConnector;

namespace Jovemnf.MySQL;

public partial class MySQL
{
    private MySqlConnection _bdConn;
    //private DataSet bdDataSet;
    private MySqlCommand cmd = null;
    private MySqlCommand _cmd
    {
        get => cmd;
        set => cmd = value;
    }
    private MySqlDataAdapter _da;
    private static MySQLData _data = default;
    MySqlTransaction trans;
    private bool _initTrans = false;

    // Configurações de Pool de Conexão - Otimizações para melhor desempenho
    /// <summary>
    /// Número máximo de conexões no pool. Aumente para aplicações com alta concorrência.
    /// </summary>
    public static uint MaximumPoolSize { get; set; } = 100;

    /// <summary>
    /// Número mínimo de conexões mantidas no pool. Conexões pré-estabelecidas reduzem latência.
    /// Recomendado: 5-10 para aplicações com tráfego constante.
    /// </summary>
    public static uint MinimumPoolSize { get; set; } = 0;

    /// <summary>
    /// Habilita/desabilita o pooling de conexões. Deve estar sempre true para melhor desempenho.
    /// </summary>
    public static bool Pooling { get; set; } = true;

    /// <summary>
    /// Timeout em segundos para obter uma conexão do pool antes de lançar exceção.
    /// </summary>
    public static uint ConnectionTimeout { get; set; } = 15;

    /// <summary>
    /// Tempo em segundos que uma conexão pode ficar idle antes de ser removida do pool.
    /// Reduz o uso de recursos mantendo apenas conexões ativas.
    /// </summary>
    public static uint ConnectionIdleTimeout { get; set; } = 180; // 3 minutos

    /// <summary>
    /// Tempo máximo de vida de uma conexão em segundos. 0 = sem limite.
    /// Útil para forçar renovação periódica de conexões antigas.
    /// </summary>
    public static uint ConnectionLifeTime { get; set; } = 0;

    /// <summary>
    /// Se true, reseta o estado da conexão ao retornar ao pool.
    /// Pode reduzir performance, mas garante estado limpo.
    /// </summary>
    public static bool ConnectionReset { get; set; } = false;

    /// <summary>
    /// Intervalo em segundos entre pacotes keepalive.
    /// </summary>
    public static uint KeepaliveInterval { get; set; } = 30;

    /// <summary>
    /// Permite uso de variáveis de usuário (@variavel) em queries. Melhora performance.
    /// </summary>
    public static bool AllowUserVariables { get; set; } = true;

    /// <summary>
    /// Habilita compressão de dados. Use apenas se latência de rede for alta (>50ms).
    /// Pode reduzir throughput em redes rápidas.
    /// </summary>
    public static bool UseCompression { get; set; } = false;

    /// <summary>
    /// Inicializa as configurações padrão de conexão usando um objeto de configuração.
    /// </summary>
    /// <param name="config">Objeto de configuração com os dados de conexão.</param>
    public static void Init(MySQLConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(config);

        _data.HOST = config.Host;
        _data.UserName = config.Username;
        _data.PassWord = config.Password;
        _data.Base = config.Database;
        _data.Charset = config.Charset;
        _data.Port = config.Port;
    }

    public static void Init(MySQLConfiguration config, PoolConfiguration pool)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(pool);

        _data.HOST = config.Host;
        _data.UserName = config.Username;
        _data.PassWord = config.Password;
        _data.Base = config.Database;
        _data.Charset = config.Charset;
        _data.Port = config.Port;

        MaximumPoolSize = pool.MaxPoolSize;
        MinimumPoolSize = pool.MinPoolSize;
        ConnectionTimeout = pool.ConnectionTimeout;
        ConnectionIdleTimeout = pool.IdleTimeout;
        ConnectionReset = pool.ConnectionReset;
        ConnectionLifeTime = pool.ConnectionLifeTime;
        KeepaliveInterval = pool.KeepaliveInterval;
    }

    /// <summary>
    /// Inicializa as configurações padrão de conexão.
    /// </summary>
    /// <param name="host">Host ou endereço IP do servidor MySQL.</param>
    /// <param name="database">Nome do banco de dados.</param>
    /// <param name="username">Nome de usuário para autenticação.</param>
    /// <param name="password">Senha para autenticação.</param>
    /// <param name="port">Porta do servidor MySQL. Padrão: 3306.</param>
    /// <param name="chatset">Charset a ser usado na conexão. Padrão: utf8.</param>
    [Obsolete("Use INIT(MySQLConfiguration config) para melhor legibilidade.")]
    public static void Init(string host, string database, string username, string password, uint port = 3306, string chatset = "utf8")
    {
        _data.HOST = host;
        _data.UserName = username;
        _data.PassWord = password;
        _data.Base = database;
        _data.Charset = chatset;
        _data.Port = port;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MySQLData
    {
        public string HOST;
        public string UserName;
        public string PassWord;
        public string Base;
        public string Charset;
        public uint Port;
    }
}
