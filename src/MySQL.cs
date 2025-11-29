using MySqlConnector;
using System;
using System.Data;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Jovemnf.MySQL
{

    public class MySQL : IDisposable
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

        public static void INIT(string host, string database, string username, string password, uint port = 3306, string chatset = "utf8")
        {
            Data.HOST = host;
            Data.UserName = username;
            Data.PassWord = password;
            Data.Base = database;
            Data.Charset = chatset;
            Data.Port = port;
        }

        public MySQL(string host, string database, string username, string password, uint port = 3306,  string chatset = "utf8")
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
                        ConnectionIdleTimeout = ConnectionIdleTimeout,
                        ConnectionLifeTime = ConnectionLifeTime,
                        ConnectionReset = ConnectionReset,
                        Keepalive = Keepalive,
                        KeepaliveInterval = KeepaliveInterval,
                        AllowUserVariables = AllowUserVariables,
                        UseCompression = UseCompression
                    };

                    //string uri = "server=" + host + ";port:" + port + ";database=" + database + ";user id=" + username + ";Max Pool Size=100;SslMode=none;pwd=" + password;
                    //this.bdDataSet = new DataSet();
                    this.bdConn = new MySqlConnection(conn_string.ToString());
                    //this.bdConn = new MySqlConnection(uri);
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

        public async Task DisposeAsync()
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
            }
            catch
            {
                throw;
            }
        }

        /*
        public int ExecuteInsert(bool lastID = true)
        {
            try
            {
                this.cmd.ExecuteNonQuery();
                if (lastID)
                {
                    return this.LastId();
                }
                return 0;
            }
            catch
            {
                throw;
            }
        }
        */

        public async Task<int> ExecuteInsertAsync(bool lastID = true)
        {
            try
            {
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

        public int ExecuteInsertSync(bool lastID = true)
        {
            try
            {
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

        public MySQLReader ExecuteQuerySync()
        {
            try
            {
                return new MySQLReader(this.cmd.ExecuteReader());
            }
            catch
            {
                throw;
            }
        }

        public async Task<MySQLReader> ExecuteQueryAsync()
        {
            try
            {
                return new MySQLReader(await this.cmd.ExecuteReaderAsync());
            }
            catch
            {
                throw;
            }
        }

        public void BeginSync()
        {
            try
            {
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
                InitTrans = true;
                trans = await bdConn.BeginTransactionAsync();
            }
            catch
            {
                throw;
            }
        }

        public void RoolBackSync()
        {
            try
            {
                trans.Rollback();
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
                await trans.RollbackAsync();
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
                trans.Commit();
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
                await trans.CommitAsync();
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
                return this.cmd.ExecuteNonQuery();
            }
            catch
            {
                throw;
            }
        }

        public async Task<int> ExecuteUpdateAsync()
        {
            try
            {
                return await this.cmd.ExecuteNonQueryAsync();
            }
            catch
            {
                throw;
            }
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
                if (this.bdConn.State != ConnectionState.Open)
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
                if (this.bdConn.State != ConnectionState.Open)
                {
                    await this.bdConn.OpenAsync();
                }
            }
            catch
            { 
                throw;
            }
        }

        public void OpenCommand(string sql)
        {
            try
            {
                this.cmd = new MySqlCommand(sql, this.bdConn);
                if (InitTrans)
                {
                    cmd.Transaction = trans;
                }
            }
            catch (Exception)
            {
                throw new MySQLConnectException("Impossível estabelecer conexão com o banco de dados");
            }
        }

        /*
        [Obsolete("setParameter is deprecated, please use SetParameter instead.")]
        public void setParameter(string param, object value)
        {
            this.cmd.Parameters.AddWithValue(param, value);
        }
        */

        public void SetParameter(string param, object value)
        {
            this.cmd.Parameters.AddWithValue(param, value);
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
                return this.cmd.CommandText;
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
