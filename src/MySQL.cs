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

        public static uint MaximumPoolSize { get; set; } = 100;
        public static bool Pooling { get; set; } = true;

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
                        Pooling = Pooling
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
                bdConn.Close();
            }
            catch
            {
                throw;
            }
        }

        public void CloseAsync()
        {
            try
            {
                bdConn.CloseAsync();
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
                }
                this.bdConn.Dispose();
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
                }
                await this.bdConn.DisposeAsync();
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
