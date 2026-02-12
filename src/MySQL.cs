using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Data;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Jovemnf.MySQL
{
    /// <summary>
    /// Classe de configuração para conexão MySQL.
    /// Agrupa todas as propriedades necessárias para estabelecer uma conexão.
    /// </summary>
    public class MySQLConfiguration
    {
        /// <summary>
        /// Host ou endereço IP do servidor MySQL.
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        /// Nome do banco de dados.
        /// </summary>
        public string Database { get; set; }

        /// <summary>
        /// Nome de usuário para autenticação.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Senha para autenticação.
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Porta do servidor MySQL. Padrão: 3306.
        /// </summary>
        public uint Port { get; set; } = 3306;

        /// <summary>
        /// Charset a ser usado na conexão. Padrão: utf8.
        /// </summary>
        public string Charset { get; set; } = "utf8";

        /// <summary>
        /// String de conexão completa. Se fornecida, outras propriedades são ignoradas.
        /// </summary>
        public string ConnectionString { get; set; }
    }

    public class MySQL : IDisposable, IAsyncDisposable
    {

        private MySqlConnection bdConn;
        //private DataSet bdDataSet;
        private MySqlCommand cmd = null;
        private MySqlDataAdapter da;
        private static MySQLData Data = default;
        MySqlTransaction trans;
        bool InitTrans = false;

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
        /// Habilita keepalive TCP para detectar conexões mortas mais rapidamente.
        /// </summary>
        public static bool Keepalive { get; set; } = true;
        
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
        public static void INIT(MySQLConfiguration config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
                
            Data.HOST = config.Host;
            Data.UserName = config.Username;
            Data.PassWord = config.Password;
            Data.Base = config.Database;
            Data.Charset = config.Charset;
            Data.Port = config.Port;
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
        public static void INIT(string host, string database, string username, string password, uint port = 3306, string chatset = "utf8")
        {
            Data.HOST = host;
            Data.UserName = username;
            Data.PassWord = password;
            Data.Base = database;
            Data.Charset = chatset;
            Data.Port = port;
        }

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
                if (bdConn is null)
                {
                    if (!string.IsNullOrEmpty(config.ConnectionString))
                    {
                        this.bdConn = new MySqlConnection(config.ConnectionString);
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

                        this.bdConn = new MySqlConnection(conn_string.ToString());
                    }
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
                if (bdConn is null)
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

                    this.bdConn = new MySqlConnection(conn_string.ToString());
                }
            }
            catch
            {
                throw;
            }
        }

        public MySQL(string stringConnect)
        {
            try
            {
                if (bdConn is null)
                {
                    //this.bdDataSet = new DataSet();
                    this.bdConn = new MySqlConnection(stringConnect);
                }
            }
            catch
            {
                throw;
            }
        }

        public MySQL()
        {
            try
            {
                if (bdConn is null)
                {
                    //string uri = "server=" + Data.HOST + ";port:" + Data.Port + ";database=" + Data.Base + ";user id=" + Data.UserName + ";Max Pool Size=100;SslMode=none;pwd=" + Data.PassWord;
                    //this.bdDataSet = new DataSet();

                    MySqlConnectionStringBuilder conn_string = new ()
                    {
                        Server = Data.HOST,
                        Port = Data.Port,
                        UserID = Data.UserName,
                        Password = Data.PassWord,
                        Database = Data.Base,
                        CharacterSet = Data.Charset,
                        SslMode = MySqlSslMode.None,
                        MaximumPoolSize = MaximumPoolSize,
                        Pooling = Pooling
                    };

                    this.bdConn = new MySqlConnection(conn_string.ToString());
                    //this.bdConn = new MySqlConnection(uri);
                }
            }
            catch
            {
                throw;
            }
        }

        public void CloseSync()
        {
            try
            {
                if (bdConn != null && bdConn.State == ConnectionState.Open)
                {
                    bdConn.Close(); // Retorna a conexão ao pool
                }
            }
            catch
            {
                throw;
            }
        }

        public async Task CloseAsync()
        {
            try
            {
                if (bdConn != null && bdConn.State == ConnectionState.Open)
                {
                    await bdConn.CloseAsync(); // Retorna a conexão ao pool
                }
            }
            catch
            {
                throw;
            }
        }

        public void CreateAdapter(string command)
        {
            this.da = new MySqlDataAdapter(command, this.bdConn);
        }

        public void Dispose()
        {
            try
            {
                if (this.cmd != null) {
                    this.cmd.Dispose();
                    this.cmd = null;
                }
                
                // Limpa transação se existir
                if (trans != null)
                {
                    trans.Dispose();
                    trans = null;
                    InitTrans = false;
                }
                
                // Garante que a conexão seja fechada antes de retornar ao pool
                if (this.bdConn != null)
                {
                    if (this.bdConn.State == ConnectionState.Open)
                    {
                        this.bdConn.Close();
                    }
                    this.bdConn.Dispose();
                    this.bdConn = null;
                }
            }
            catch
            {
                throw;
            }
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                if (this.cmd != null)
                {
                    await this.cmd.DisposeAsync();
                    this.cmd = null;
                }
                
                // Garante que a conexão seja fechada antes de retornar ao pool
                if (this.bdConn != null)
                {
                    if (this.bdConn.State == ConnectionState.Open)
                    {
                        await this.bdConn.CloseAsync();
                    }
                    await this.bdConn.DisposeAsync();
                    this.bdConn = null;
                }

                // Limpa transação se existir
                if (trans != null)
                {
                    await trans.DisposeAsync();
                    trans = null;
                    InitTrans = false;
                }
            }
            catch
            {
                throw;
            }
        }

        public async Task<int> ExecuteInsertAsync(bool lastID = true)
        {
            try
            {
                if (cmd == null)
                {
                    throw new InvalidOperationException("Comando não foi inicializado. Chame OpenCommand primeiro.");
                }
                await cmd.ExecuteNonQueryAsync();
                if (lastID)
                {
                    return await LastIdAsync();
                }
                return 0;
            }
            catch
            {
                throw;
            }
        }

        public async Task<long> ExecuteInsertAsyncLong(bool lastID = true)
        {
            try
            {
                if (cmd == null)
                {
                    throw new InvalidOperationException("Comando não foi inicializado. Chame OpenCommand primeiro.");
                }
                await cmd.ExecuteNonQueryAsync();
                if (lastID)
                {
                    return await LastIdAsyncLong();
                }
                return 0;
            }
            catch
            {
                throw;
            }
        }

        public async Task<long> ExecuteInsertAsync(InsertQueryBuilder builder, bool lastID = true)
        {
            var (sql, command) = builder.Build();
            this.cmd = command;
            this.cmd.Connection = this.bdConn;
            
            if (this.trans != null)
                this.cmd.Transaction = this.trans;

            await this.cmd.ExecuteNonQueryAsync();

            if (lastID)
                return await LastIdAsyncLong();

            return 0;
        }

        public int ExecuteInsertSync(bool lastID = true)
        {
            try
            {
                if (cmd == null)
                {
                    throw new InvalidOperationException("Comando não foi inicializado. Chame OpenCommand primeiro.");
                }
                cmd.ExecuteNonQuery();
                if (lastID)
                {
                    return LastIdSync();
                }
                return 0;
            }
            catch
            {
                throw;
            }
        }

        public long ExecuteInsertSync(InsertQueryBuilder builder, bool lastID = true)
        {
            var (sql, command) = builder.Build();
            this.cmd = command;
            this.cmd.Connection = this.bdConn;
            
            if (this.trans != null)
                this.cmd.Transaction = this.trans;

            this.cmd.ExecuteNonQuery();

            if (lastID)
                return LastIdSync();

            return 0;
        }

        public MySQLReader ExecuteQuerySync()
        {
            try
            {
                if (this.cmd == null)
                {
                    throw new InvalidOperationException("Comando não foi inicializado. Chame OpenCommand primeiro.");
                }
                return new MySQLReader(this.cmd.ExecuteReader());
            }
            catch
            {
                throw;
            }
        }

        public MySQLReader ExecuteQuerySync(SelectQueryBuilder builder)
        {
            var (sql, command) = builder.Build();
            this.cmd = command;
            this.cmd.Connection = this.bdConn;
            if (this.trans != null) this.cmd.Transaction = this.trans;
            return ExecuteQuerySync();
        }

        public async Task<MySQLReader> ExecuteQueryAsync()
        {
            try
            {
                if (this.cmd == null)
                {
                    throw new InvalidOperationException("Comando não foi inicializado. Chame OpenCommand primeiro.");
                }
                return new MySQLReader(await this.cmd.ExecuteReaderAsync());
            }
            catch
            {
                throw;
            }
        }

        public async Task<MySQLReader> ExecuteQueryAsync(SelectQueryBuilder builder)
        {
            var (sql, command) = builder.Build();
            this.cmd = command;
            this.cmd.Connection = this.bdConn;
            if (this.trans != null) this.cmd.Transaction = this.trans;
            return await ExecuteQueryAsync();
        }

        public void BeginSync()
        {
            try
            {
                if (this.bdConn == null)
                {
                    throw new InvalidOperationException("Conexão não foi inicializada.");
                }
                InitTrans = true;
                trans = this.bdConn.BeginTransaction();
            }
            catch
            {
                throw;
            }
        }

        public async Task BeginAsync()
        {
            try
            {
                if (bdConn == null)
                {
                    throw new InvalidOperationException("Conexão não foi inicializada.");
                }
                InitTrans = true;
                trans = await bdConn.BeginTransactionAsync();
            }
            catch
            {
                throw;
            }
        }

        public void RollbackSync()
        {
            try
            {
                if (trans != null)
                {
                    trans.Rollback();
                    trans.Dispose();
                    trans = null;
                    InitTrans = false;
                }
            }
            catch
            {
                throw;
            }
        }

        public async Task RollbackAsync()
        {
            try
            {
                if (trans != null)
                {
                    await trans.RollbackAsync();
                    await trans.DisposeAsync();
                    trans = null;
                    InitTrans = false;
                }
            }
            catch
            {
                throw;
            }
        }

        public void CommitSync()
        {
            try
            {
                if (trans != null)
                {
                    trans.Commit();
                    trans.Dispose();
                    trans = null;
                    InitTrans = false;
                }
            }
            catch
            {
                throw;
            }
        }

        public async Task CommitAsync()
        {
            try
            {
                if (trans != null)
                {
                    await trans.CommitAsync();
                    await trans.DisposeAsync();
                    trans = null;
                    InitTrans = false;
                }
            }
            catch
            {
                throw;
            }
        }

        public int ExecuteUpdateSync()
        {
            try
            {
                if (this.cmd == null)
                {
                    throw new InvalidOperationException("Comando não foi inicializado. Chame OpenCommand primeiro.");
                }
                return this.cmd.ExecuteNonQuery();
            }
            catch
            {
                throw;
            }
        }

        public int ExecuteUpdateSync(UpdateQueryBuilder builder)
        {
            var (sql, command) = builder.Build();
            this.cmd = command;
            this.cmd.Connection = this.bdConn;
            
            if (this.trans != null)
                this.cmd.Transaction = this.trans;

            return ExecuteUpdateSync();
        }

        public async Task<int> ExecuteUpdateAsync()
        {
            try
            {
                if (this.cmd == null)
                {
                    throw new InvalidOperationException("Comando não foi inicializado. Chame OpenCommand primeiro.");
                }
                return await this.cmd.ExecuteNonQueryAsync();
            }
            catch
            {
                throw;
            }
        }

        public async Task<int> ExecuteUpdateAsync(UpdateQueryBuilder builder)
        {
            var (sql, command) = builder.Build();
            this.cmd = command;
            this.cmd.Connection = this.bdConn;
            
            if (this.trans != null)
                this.cmd.Transaction = this.trans;

            return await ExecuteUpdateAsync();
        }

        /*
        private int LastId()
        {
            string str;
            try
            {
                this.cmd.CommandText = "SELECT LAST_INSERT_ID()";
                str = this.cmd.ExecuteScalar().ToString();
            }
            catch
            {
                throw;
            }
            return Convert.ToInt32(str);
        }
        */

        private async Task<int> LastIdAsync()
        {
            try
            {
                if (this.cmd == null)
                {
                    throw new InvalidOperationException("Comando não foi inicializado. Chame OpenCommand primeiro.");
                }
                this.cmd.CommandText = "SELECT LAST_INSERT_ID()";
                object result = await this.cmd.ExecuteScalarAsync();
                return result != null ? Convert.ToInt32(result) : 0;
            }
            catch
            {
                throw;
            }
        }

        private async Task<long> LastIdAsyncLong()
        {
            try
            {
                if (this.cmd == null)
                {
                    throw new InvalidOperationException("Comando não foi inicializado. Chame OpenCommand primeiro.");
                }
                this.cmd.CommandText = "SELECT LAST_INSERT_ID()";
                object result = await this.cmd.ExecuteScalarAsync();
                return result != null ? Convert.ToInt64(result) : 0;
            }
            catch
            {
                throw;
            }
        }

        private int LastIdSync()
        {
            try
            {
                if (this.cmd == null)
                {
                    throw new InvalidOperationException("Comando não foi inicializado. Chame OpenCommand primeiro.");
                }
                this.cmd.CommandText = "SELECT LAST_INSERT_ID()";
                object result = this.cmd.ExecuteScalar();
                return result != null ? Convert.ToInt32(result) : 0;
            }
            catch
            {
                throw;
            }
        }

        public void OpenSync()
        {
            try
            {
                if (this.bdConn != null && this.bdConn.State != ConnectionState.Open)
                {
                    this.bdConn.Open();
                }
            }
            catch
            {
                throw;
            }
        }

        public async Task OpenAsync()
        {
            try
            {
                if (this.bdConn != null && this.bdConn.State != ConnectionState.Open)
                {
                    await this.bdConn.OpenAsync();
                }
            }
            catch
            { 
                throw;
            }
        }

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
                if (this.bdConn == null)
                {
                    throw new InvalidOperationException("Conexão não foi inicializada.");
                }

                // Dispose do comando anterior se existir
                if (this.cmd != null)
                {
                    this.cmd.Dispose();
                }

                this.cmd = new MySqlCommand(sql, this.bdConn);
                if (InitTrans && trans != null)
                {
                    cmd.Transaction = trans;
                }
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
            if (this.cmd != null)
            {
                this.cmd.CommandTimeout = commandTimeout;
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
            if (this.cmd == null)
            {
                throw new InvalidOperationException("Comando não foi inicializado. Chame OpenCommand primeiro.");
            }
            this.cmd.Parameters.AddWithValue(param, value ?? DBNull.Value);
        }

        /// <summary>
        /// Adiciona um parâmetro ao comando SQL com tipo específico.
        /// </summary>
        /// <param name="param">Nome do parâmetro (ex: @nome).</param>
        /// <param name="value">Valor do parâmetro.</param>
        /// <param name="dbType">Tipo do parâmetro no banco de dados.</param>
        public void SetParameter(string param, object value, MySqlDbType dbType)
        {
            if (this.cmd == null)
            {
                throw new InvalidOperationException("Comando não foi inicializado. Chame OpenCommand primeiro.");
            }
            var parameter = new MySqlParameter(param, dbType)
            {
                Value = value ?? DBNull.Value
            };
            this.cmd.Parameters.Add(parameter);
        }

        public MySqlDataAdapter Adapter
        {
            get
            {
                return this.da;
            }
        }

        public string CommandText
        {
            get
            {
                return this.cmd?.CommandText ?? string.Empty;
            }
        }

        /// <summary>
        /// Obtém o estado atual da conexão.
        /// </summary>
        public ConnectionState State => bdConn?.State ?? ConnectionState.Closed;

        /// <summary>
        /// Indica se há uma transação ativa.
        /// </summary>
        public bool HasActiveTransaction => InitTrans && trans != null;

        /// <summary>
        /// Executa um comando e retorna um único valor (ExecuteScalar).
        /// </summary>
        /// <param name="sql">Comando SQL a ser executado.</param>
        /// <returns>Primeira coluna da primeira linha do resultado ou null.</returns>
        public object ExecuteScalarSync(string sql)
        {
            if (string.IsNullOrEmpty(sql))
                throw new ArgumentException("SQL não pode ser vazio.", nameof(sql));

            if (bdConn == null)
                throw new InvalidOperationException("Conexão não foi inicializada.");

            try
            {
                using (var cmd = new MySqlCommand(sql, bdConn))
                {
                    if (InitTrans && trans != null)
                    {
                        cmd.Transaction = trans;
                    }
                    return cmd.ExecuteScalar();
                }
            }
            catch (Exception ex)
            {
                throw new MySQLConnectException($"Erro ao executar ExecuteScalar: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Executa um comando e retorna um único valor de forma assíncrona (ExecuteScalar).
        /// </summary>
        /// <param name="sql">Comando SQL a ser executado.</param>
        /// <returns>Task com primeira coluna da primeira linha do resultado ou null.</returns>
        public async Task<object> ExecuteScalarAsync(string sql)
        {
            if (string.IsNullOrEmpty(sql))
                throw new ArgumentException("SQL não pode ser vazio.", nameof(sql));

            if (bdConn == null)
                throw new InvalidOperationException("Conexão não foi inicializada.");

            try
            {
                using (var cmd = new MySqlCommand(sql, bdConn))
                {
                    if (InitTrans && trans != null)
                    {
                        cmd.Transaction = trans;
                    }
                    return await cmd.ExecuteScalarAsync();
                }
            }
            catch (Exception ex)
            {
                throw new MySQLConnectException($"Erro ao executar ExecuteScalarAsync: {ex.Message}", ex);
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

            if (bdConn == null)
                throw new InvalidOperationException("Conexão não foi inicializada.");

            bool wasTransactionActive = InitTrans;
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

            if (bdConn == null)
                throw new InvalidOperationException("Conexão não foi inicializada.");

            bool wasTransactionActive = InitTrans;
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

        /// <summary>
        /// Adiciona múltiplos parâmetros ao comando de uma vez.
        /// </summary>
        /// <param name="parameters">Dicionário com nome do parâmetro como chave e valor como valor.</param>
        public void SetParameters(Dictionary<string, object> parameters)
        {
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            if (cmd == null)
                throw new InvalidOperationException("Comando não foi inicializado. Chame OpenCommand primeiro.");

            foreach (var param in parameters)
            {
                cmd.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
            }
        }

        /// <summary>
        /// Limpa todos os parâmetros do comando atual.
        /// </summary>
        public void ClearParameters()
        {
            if (cmd != null)
            {
                cmd.Parameters.Clear();
            }
        }

        /// <summary>
        /// Testa a conexão com o banco de dados.
        /// </summary>
        /// <returns>True se a conexão está funcionando, caso contrário False.</returns>
        public bool TestConnection()
        {
            try
            {
                if (bdConn == null)
                    return false;

                bool wasOpen = bdConn.State == ConnectionState.Open;
                if (!wasOpen)
                {
                    OpenSync();
                }

                using (var cmd = new MySqlCommand("SELECT 1", bdConn))
                {
                    cmd.ExecuteScalar();
                }

                if (!wasOpen)
                {
                    CloseSync();
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Testa a conexão com o banco de dados de forma assíncrona.
        /// </summary>
        /// <returns>Task com True se a conexão está funcionando, caso contrário False.</returns>
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                if (bdConn == null)
                    return false;

                bool wasOpen = bdConn.State == ConnectionState.Open;
                if (!wasOpen)
                {
                    await OpenAsync();
                }

                using (var cmd = new MySqlCommand("SELECT 1", bdConn))
                {
                    await cmd.ExecuteScalarAsync();
                }

                if (!wasOpen)
                {
                    await CloseAsync();
                }

                return true;
            }
            catch
            {
                return false;
            }
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
}
