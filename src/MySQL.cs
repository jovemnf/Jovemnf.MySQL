using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Runtime.InteropServices;
using System.Xml;
using Microsoft.Win32;

namespace Jovemnf.MySQL
{

    public class MySQL : IDisposable
    {

        private MySqlConnection bdConn;
        private DataSet bdDataSet;
        private MySqlCommand cmd;
        private MySqlDataAdapter da;
        private static MySQLData Data;
        MySqlTransaction trans;
        bool InitTrans = false;

        public static void INIT(string host, string database, string username, string password)
        {
            Data.HOST = host;
            Data.UserName = username;
            Data.PassWord = password;
            Data.Base = database;
        }

        public MySQL(string host, string database, string username, string password)
        {
            try
            {
                if (!(this.bdConn is MySqlConnection))
                {
                    string uri = "server=" + host + ";database=" + database + ";Command Timeout=28800;uid=" + username + ";Max Pool Size=45;SslMode=none;pwd=" + password;
                    this.bdDataSet = new DataSet();
                    this.bdConn = new MySqlConnection(uri);
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
                    this.bdDataSet = new DataSet();
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
                    string uri = "server=" + Data.HOST + ";database=" + Data.Base + ";Command Timeout=28800;uid=" + Data.UserName + ";Max Pool Size=45;SslMode=none;pwd=" + Data.PassWord;
                    this.bdDataSet = new DataSet();
                    this.bdConn = new MySqlConnection(uri);
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
                if (this.bdConn.State == ConnectionState.Open)
                {
                    this.bdConn.Close();
                    this.bdConn = null;
                }
            }
            catch (Exception ex)
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
                if (this.bdConn.State == ConnectionState.Open)
                {
                    this.bdConn.Dispose();
                }
            }
            catch
            {
                //throw new MySQLCloseException("Impossível fechar conexão com o banco de dados");
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
        }

    }
}
