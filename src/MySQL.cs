﻿using MySql.Data.MySqlClient;
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
        private static MySQLData Data;
        MySqlTransaction trans;
        bool InitTrans = false;

        MySqlConnection Connection {
            get {
                return this.bdConn;
            }
        }

        MySqlCommand Command {
            get {
                return this.cmd;
            }
        }

        static uint MaximumPoolSize { get; set; } = 100;
        static bool Pooling { get; set; } = false;

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
                if (!(this.bdConn is MySqlConnection))
                {

                    MySqlConnectionStringBuilder conn_string = new MySqlConnectionStringBuilder();
                    conn_string.Server = host;
                    conn_string.UserID = username;
                    conn_string.Password = password;
                    conn_string.Database = database;
                    conn_string.CharacterSet = chatset;
                    conn_string.Port = port;
                    conn_string.SslMode = MySqlSslMode.None;
                    conn_string.MaximumPoolSize = MaximumPoolSize;
                    conn_string.Pooling = Pooling;

                    //string uri = "server=" + host + ";Charset= " + chatset + ";database=" + database + ";Command Timeout=28800;uid=" + username + ";Max Pool Size=45;SslMode=none;pwd=" + password;
                    //this.bdDataSet = new DataSet();
                    this.bdConn = new MySqlConnection(conn_string.ToString());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.InnerException + ex.Message);
            }
        }

        public MySQL(string stringConnect)
        {
            try
            {
                if (!(this.bdConn is MySqlConnection))
                {
                    //this.bdDataSet = new DataSet();
                    this.bdConn = new MySqlConnection(stringConnect);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public MySQL()
        {
            try
            {
                if (!(this.bdConn is MySqlConnection))
                {
                    //string uri = "server=" + Data.HOST + ";Charset= " + Data.Charset + ";database=" + Data.Base + ";Command Timeout=28800;uid=" + Data.UserName + ";Max Pool Size=45;SslMode=none;pwd=" + Data.PassWord;
                    //this.bdDataSet = new DataSet();

                    MySqlConnectionStringBuilder conn_string = new MySqlConnectionStringBuilder();
                    conn_string.Server = Data.HOST;
                    conn_string.UserID = Data.UserName;
                    conn_string.Password = Data.PassWord;
                    conn_string.Database = Data.Base;
                    conn_string.CharacterSet = Data.Charset;
                    conn_string.SslMode = MySqlSslMode.None;
                    conn_string.Port = Data.Port;
                    conn_string.MaximumPoolSize = MaximumPoolSize;
                    conn_string.Pooling = Pooling;

                    this.bdConn = new MySqlConnection(conn_string.ToString());
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        
        public void Close()
        {
            try
            {
                //if (this.bdConn.State == ConnectionState.Open)
                //{
                    this.bdConn.Close();
                //    this.bdConn = null;
                //}
            }
            catch (Exception)
            {
                throw new MySQLCloseException("Impossível fechar conexão com o banco de dados");
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
            catch (Exception e)
            {
                throw e;
            }
        }

        public int ExecuteInsert()
        {
            try
            {
                this.cmd.ExecuteNonQuery();
            }
            catch (MySqlException ex)
            {
                throw ex;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return this.LastId();
        }

        public async Task<int> ExecuteInsertAsync()
        {
            try
            {
                await this.cmd.ExecuteNonQueryAsync();
            }
            catch (MySqlException ex)
            {
                throw ex;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return this.LastId();
        }

        public MySQLReader ExecuteQuery()
        {
            MySQLReader CS10000;
            try
            {
                CS10000 = new MySQLReader(this.cmd.ExecuteReader());
            }
            catch (MySqlException ex)
            {
                throw ex;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return CS10000;
        }

        public async Task<MySQLReader> ExecuteQueryAsync()
        {
            try
            {
                return new MySQLReader(await this.cmd.ExecuteReaderAsync());
            }
            catch (MySqlException ex)
            {
                throw ex;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void Begin()
        {
            try
            {
                InitTrans = true;
                trans = this.bdConn.BeginTransaction();
            }
            catch (MySqlException ex)
            {
                throw ex;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void RoolBack()
        {
            try
            {
                trans.Rollback();
            }
            catch (MySqlException ex)
            {
                throw ex;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void Commit()
        {
            try
            {
                trans.Commit();
            }
            catch (MySqlException ex)
            {
                throw ex;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public int ExecuteUpdate()
        {
            try
            {
                return this.cmd.ExecuteNonQuery();
            }
            catch (MySqlException ex)
            {
                throw ex;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<int> ExecuteUpdateAsync()
        {
            try
            {
                return await this.cmd.ExecuteNonQueryAsync();
            }
            catch (MySqlException ex)
            {
                throw ex;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private int LastId()
        {
            string str;
            try
            {
                this.cmd.CommandText = "SELECT LAST_INSERT_ID()";
                str = this.cmd.ExecuteScalar().ToString();
            }
            catch (MySqlException ex)
            {
                throw ex;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return Convert.ToInt32(str);
        }

        public void open()
        {
            try
            {
                if (this.bdConn.State != ConnectionState.Open)
                {
                    this.bdConn.Open();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.InnerException + ex.Message);
                throw new MySQLConnectException("Impossível estabelecer conexão com o banco de dados");
            }
        }

        public async Task openAsync()
        {
            try
            {
                if (this.bdConn.State != ConnectionState.Open)
                {
                    await this.bdConn.OpenAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.InnerException + ex.Message);
                throw new MySQLConnectException("Impossível estabelecer conexão com o banco de dados");
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

        public void setParameter(string param, object value)
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
