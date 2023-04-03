//using MySql.Data.MySqlClient;
using MySqlConnector;
using System;
using System.Collections.Generic;
using Jovemnf.DateTimeStamp;
using System.Data.Common;
using System.Threading.Tasks;

namespace Jovemnf.MySQL
{

    public class MySQLReader : IDisposable
    {
        private DbDataReader dr;

        public MySQLReader(DbDataReader _dr)
        {
            this.dr = _dr;
        }

        public void Dispose()
        {
            try
            {
                this.dr.Dispose();
            }
            catch
            {
                throw new MySQLCloseException("Impossível fechar conexão com o banco de dados");
            }
        }

        public object Get(string column)
        {
            try
            {
                return this.dr[column];
            }
            catch
            {
                throw new Exception("Impossível Fazer um Get");
            }
        }

        public bool GetTinyInt(string column)
        {
            bool aux = false;
            try
            {
                aux = Convert.ToBoolean( this.dr[column].ToString() );
            }
            catch { }
            return aux;
        }

        public bool GetBoolean(string column)
        {
            try
            {
                return TryParse.ToBoolean(this.dr[column]);
            }
            catch
            {
                throw;
            }
        }

        public DateTime GetDataTime(string column)
        {
            try
            {
                return Convert.ToDateTime(this.dr[column]);
            }
            catch
            {
                throw;
            }
        }

        public MyDateTime GetMyDataTime(string column)
        {
            try
            {
                return new MyDateTime( Convert.ToDateTime(this.dr[column]) );
            }
            catch
            {
                return new MyDateTime();
            }
        }

        public MyDate GetMyData(string column)
        {
            try
            {
                return new MyDate(Convert.ToDateTime(this.dr[column]));
            }
            catch
            {
                return new MyDate();
            }
        }

        public decimal GetDecimal(string column)
        {
            try
            {
                return TryParse.ToDecimal(this.dr[column]);
            }
            catch
            {
                return 0;
            }
        }

        public double GetDouble(string column)
        {
            try
            {
                return TryParse.ToDouble(this.dr[column]);
            }
            catch
            {
                return 0.00;
            }
        }

        public MySQLArrayReader GetMySQLArrayReader()
        {
            try
            {
                return new MySQLArrayReader(dr);
            }
            catch
            {
                return null;
            }
        }

        public int GetInteger(string column, int _default = 0)
        {
            try
            {
                return TryParse.ToInt32(this.dr[column]);
            }
            catch
            {
                return _default;
            }
        }

        public long GetLong(string column)
        {
            try
            {
                return TryParse.ToLong(this.dr[column]);
            }
            catch
            {
                return 0;
            }
        }

        public string GetString(string column, string vdefault = null)
        {
            string CS10000;
            try
            {
                CS10000 = this.dr[column].ToString();
                if (CS10000 == null)
                {
                    CS10000 = vdefault;
                }
            }
            catch
            {
                throw new Exception("Impossível Fazer um Get String");
            }
            return CS10000;
        }

        public byte[] GetByteArray(string column)
        {
            byte[] CS10000;
            try
            {
                CS10000 = (byte[]) this.dr[column];
            }
            catch
            {
                throw new Exception("Impossível Fazer um Get String");
            }
            return CS10000;
        }

        public bool Read()
        {
            try
            {
                return this.dr.Read();
            }
            catch
            {
                throw new Exception("Impossível Fazer um Read");
            }
        }

        public Task<bool> ReadAsync()
        {
            try
            {
                return this.dr.ReadAsync();
            }
            catch
            {
                throw new Exception("Impossível Fazer um Read");
            }
        }
    }

    public class MySQLArrayReader
    {
        public Dictionary<string, object> List = new Dictionary<string, object>();

        public MySQLArrayReader(DbDataReader dr)
        {
            foreach (KeyValuePair<string, object> pair in dr)
            {
                List.Add(pair.Key, pair.Value);
            }
        }
    }
}