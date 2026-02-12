//using MySql.Data.MySqlClient;
using MySqlConnector;
using System;
using System.Collections.Generic;
using Jovemnf.DateTimeStamp;
using System.Data.Common;
using System.Threading.Tasks;

namespace Jovemnf.MySQL
{

    public class MySQLReader : IDisposable, IAsyncDisposable
    {
        private DbDataReader dr;

        public MySQLReader(DbDataReader _dr)
        {
            this.dr = _dr ?? throw new ArgumentNullException(nameof(_dr));
        }

        /// <summary>
        /// Obtém o número de colunas no resultado.
        /// </summary>
        public int FieldCount => dr?.FieldCount ?? 0;

        /// <summary>
        /// Indica se o resultado contém uma ou mais linhas.
        /// </summary>
        public bool HasRows => dr?.HasRows ?? false;

        /// <summary>
        /// Verifica se uma coluna existe no resultado.
        /// </summary>
        /// <param name="columnName">Nome da coluna.</param>
        /// <returns>True se a coluna existe, caso contrário False.</returns>
        public bool ColumnExists(string columnName)
        {
            if (dr == null || string.IsNullOrEmpty(columnName))
                return false;

            try
            {
                dr.GetOrdinal(columnName);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Verifica se o valor da coluna é NULL.
        /// </summary>
        /// <param name="column">Nome da coluna.</param>
        /// <returns>True se o valor é NULL, caso contrário False.</returns>
        public bool IsNull(string column)
        {
            if (dr == null || string.IsNullOrEmpty(column))
                return true;

            try
            {
                int ordinal = dr.GetOrdinal(column);
                return dr.IsDBNull(ordinal);
            }
            catch
            {
                return true;
            }
        }

        /// <summary>
        /// Verifica se o valor da coluna é NULL usando índice.
        /// </summary>
        /// <param name="index">Índice da coluna (baseado em zero).</param>
        /// <returns>True se o valor é NULL, caso contrário False.</returns>
        public bool IsNull(int index)
        {
            if (dr == null || index < 0 || index >= dr.FieldCount)
                return true;

            try
            {
                return dr.IsDBNull(index);
            }
            catch
            {
                return true;
            }
        }

        /// <summary>
        /// Obtém o nome de uma coluna pelo índice.
        /// </summary>
        /// <param name="index">Índice da coluna (baseado em zero).</param>
        /// <returns>Nome da coluna.</returns>
        public string GetColumnName(int index)
        {
            if (dr == null || index < 0 || index >= dr.FieldCount)
                throw new ArgumentOutOfRangeException(nameof(index));

            return dr.GetName(index);
        }

        /// <summary>
        /// Obtém uma lista com todos os nomes das colunas.
        /// </summary>
        /// <returns>Lista de nomes das colunas.</returns>
        public List<string> GetColumnNames()
        {
            var columns = new List<string>();
            if (dr == null)
                return columns;

            try
            {
                for (int i = 0; i < dr.FieldCount; i++)
                {
                    columns.Add(dr.GetName(i));
                }
            }
            catch { }

            return columns;
        }

        public void Dispose()
        {
            try
            {
                if (dr != null)
                {
                    dr.Dispose();
                    dr = null;
                }
            }
            catch
            {
                throw new MySQLCloseException("Impossível fechar conexão com o banco de dados");
            }
        }

        /// <summary>
        /// Libera os recursos de forma assíncrona.
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            try
            {
                if (dr != null)
                {
                    if (dr is IAsyncDisposable asyncDisposable)
                    {
                        await asyncDisposable.DisposeAsync();
                    }
                    else
                    {
                        dr.Dispose();
                    }
                    dr = null;
                }
            }
            catch
            {
                throw new MySQLCloseException("Impossível fechar conexão com o banco de dados");
            }
        }

        /// <summary>
        /// Obtém o valor de uma coluna como objeto.
        /// </summary>
        /// <param name="column">Nome da coluna.</param>
        /// <returns>Valor da coluna ou null se não encontrado.</returns>
        public object Get(string column)
        {
            if (dr == null)
                throw new InvalidOperationException("Reader não foi inicializado.");

            if (string.IsNullOrEmpty(column))
                throw new ArgumentException("Nome da coluna não pode ser vazio.", nameof(column));

            try
            {
                int ordinal = dr.GetOrdinal(column);
                return dr.IsDBNull(ordinal) ? null : dr.GetValue(ordinal);
            }
            catch (Exception ex)
            {
                throw new Exception($"Impossível obter valor da coluna '{column}': {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Obtém o valor de uma coluna pelo índice.
        /// </summary>
        /// <param name="index">Índice da coluna (baseado em zero).</param>
        /// <returns>Valor da coluna ou null se não encontrado.</returns>
        public object Get(int index)
        {
            if (dr == null)
                throw new InvalidOperationException("Reader não foi inicializado.");

            if (index < 0 || index >= dr.FieldCount)
                throw new ArgumentOutOfRangeException(nameof(index));

            try
            {
                return dr.IsDBNull(index) ? null : dr.GetValue(index);
            }
            catch (Exception ex)
            {
                throw new Exception($"Impossível obter valor da coluna no índice {index}: {ex.Message}", ex);
            }
        }

        public bool GetTinyInt(string column)
        {
            bool aux = false;
            try
            {
                aux = TryParse.ToBoolean(this.dr[column]);
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

        /// <summary>
        /// Obtém o valor de uma coluna como DateTime.
        /// </summary>
        /// <param name="column">Nome da coluna.</param>
        /// <returns>Valor da coluna como DateTime.</returns>
        public DateTime GetDataTime(string column)
        {
            if (dr == null || string.IsNullOrEmpty(column))
                throw new InvalidOperationException("Reader não foi inicializado ou nome da coluna é inválido.");

            try
            {
                int ordinal = dr.GetOrdinal(column);
                if (dr.IsDBNull(ordinal))
                    throw new InvalidOperationException($"A coluna '{column}' contém um valor NULL.");

                return Convert.ToDateTime(dr.GetValue(ordinal));
            }
            catch (Exception ex)
            {
                throw new Exception($"Impossível obter DateTime da coluna '{column}': {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Obtém o valor de uma coluna como DateTime nullable.
        /// </summary>
        /// <param name="column">Nome da coluna.</param>
        /// <returns>Valor da coluna como DateTime nullable ou null se for NULL.</returns>
        public DateTime? GetNullableDateTime(string column)
        {
            if (dr == null || string.IsNullOrEmpty(column))
                return null;

            try
            {
                int ordinal = dr.GetOrdinal(column);
                if (dr.IsDBNull(ordinal))
                    return null;

                return Convert.ToDateTime(dr.GetValue(ordinal));
            }
            catch
            {
                return null;
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

        /// <summary>
        /// Obtém o valor de uma coluna como decimal.
        /// </summary>
        /// <param name="column">Nome da coluna.</param>
        /// <param name="default">Valor padrão se a coluna for NULL ou não encontrada.</param>
        /// <returns>Valor da coluna como decimal ou o valor padrão.</returns>
        public decimal GetDecimal(string column, decimal @default = 0)
        {
            if (dr == null || string.IsNullOrEmpty(column))
                return @default;

            try
            {
                int ordinal = dr.GetOrdinal(column);
                if (dr.IsDBNull(ordinal))
                    return @default;

                return TryParse.ToDecimal(dr.GetValue(ordinal));
            }
            catch
            {
                return @default;
            }
        }

        /// <summary>
        /// Obtém o valor de uma coluna como decimal nullable.
        /// </summary>
        /// <param name="column">Nome da coluna.</param>
        /// <returns>Valor da coluna como decimal nullable ou null se for NULL.</returns>
        public decimal? GetNullableDecimal(string column)
        {
            if (dr == null || string.IsNullOrEmpty(column))
                return null;

            try
            {
                int ordinal = dr.GetOrdinal(column);
                if (dr.IsDBNull(ordinal))
                    return null;

                return TryParse.ToDecimal(dr.GetValue(ordinal));
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Obtém o valor de uma coluna como double.
        /// </summary>
        /// <param name="column">Nome da coluna.</param>
        /// <param name="default">Valor padrão se a coluna for NULL ou não encontrada.</param>
        /// <returns>Valor da coluna como double ou o valor padrão.</returns>
        public double GetDouble(string column, double @default = 0.0)
        {
            if (dr == null || string.IsNullOrEmpty(column))
                return @default;

            try
            {
                int ordinal = dr.GetOrdinal(column);
                if (dr.IsDBNull(ordinal))
                    return @default;

                return TryParse.ToDouble(dr.GetValue(ordinal));
            }
            catch
            {
                return @default;
            }
        }

        /// <summary>
        /// Obtém o valor de uma coluna como double nullable.
        /// </summary>
        /// <param name="column">Nome da coluna.</param>
        /// <returns>Valor da coluna como double nullable ou null se for NULL.</returns>
        public double? GetNullableDouble(string column)
        {
            if (dr == null || string.IsNullOrEmpty(column))
                return null;

            try
            {
                int ordinal = dr.GetOrdinal(column);
                if (dr.IsDBNull(ordinal))
                    return null;

                return TryParse.ToDouble(dr.GetValue(ordinal));
            }
            catch
            {
                return null;
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

        /// <summary>
        /// Obtém o valor de uma coluna como inteiro.
        /// </summary>
        /// <param name="column">Nome da coluna.</param>
        /// <param name="default">Valor padrão se a coluna for NULL ou não encontrada.</param>
        /// <returns>Valor da coluna como inteiro ou o valor padrão.</returns>
        public int GetInteger(string column, int @default = 0)
        {
            if (dr == null || string.IsNullOrEmpty(column))
                return @default;

            try
            {
                int ordinal = dr.GetOrdinal(column);
                if (dr.IsDBNull(ordinal))
                    return @default;

                return TryParse.ToInt32(dr.GetValue(ordinal));
            }
            catch
            {
                return @default;
            }
        }

        /// <summary>
        /// Obtém o valor de uma coluna como inteiro usando índice.
        /// </summary>
        /// <param name="index">Índice da coluna (baseado em zero).</param>
        /// <param name="default">Valor padrão se a coluna for NULL.</param>
        /// <returns>Valor da coluna como inteiro ou o valor padrão.</returns>
        public int GetInteger(int index, int @default = 0)
        {
            if (dr == null || index < 0 || index >= dr.FieldCount)
                return @default;

            try
            {
                if (dr.IsDBNull(index))
                    return @default;

                return TryParse.ToInt32(dr.GetValue(index));
            }
            catch
            {
                return @default;
            }
        }

        /// <summary>
        /// Obtém o valor de uma coluna como inteiro nullable.
        /// </summary>
        /// <param name="column">Nome da coluna.</param>
        /// <returns>Valor da coluna como inteiro nullable ou null se for NULL.</returns>
        public int? GetNullableInteger(string column)
        {
            if (dr == null || string.IsNullOrEmpty(column))
                return null;

            try
            {
                int ordinal = dr.GetOrdinal(column);
                if (dr.IsDBNull(ordinal))
                    return null;

                return TryParse.ToInt32(dr.GetValue(ordinal));
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Obtém o valor de uma coluna como long.
        /// </summary>
        /// <param name="column">Nome da coluna.</param>
        /// <param name="default">Valor padrão se a coluna for NULL ou não encontrada.</param>
        /// <returns>Valor da coluna como long ou o valor padrão.</returns>
        public long GetLong(string column, long @default = 0)
        {
            if (dr == null || string.IsNullOrEmpty(column))
                return @default;

            try
            {
                int ordinal = dr.GetOrdinal(column);
                if (dr.IsDBNull(ordinal))
                    return @default;

                return TryParse.ToLong(dr.GetValue(ordinal));
            }
            catch
            {
                return @default;
            }
        }

        /// <summary>
        /// Obtém o valor de uma coluna como long usando índice.
        /// </summary>
        /// <param name="index">Índice da coluna (baseado em zero).</param>
        /// <param name="default">Valor padrão se a coluna for NULL.</param>
        /// <returns>Valor da coluna como long ou o valor padrão.</returns>
        public long GetLong(int index, long @default = 0)
        {
            if (dr == null || index < 0 || index >= dr.FieldCount)
                return @default;

            try
            {
                if (dr.IsDBNull(index))
                    return @default;

                return TryParse.ToLong(dr.GetValue(index));
            }
            catch
            {
                return @default;
            }
        }

        /// <summary>
        /// Obtém o valor de uma coluna como long nullable.
        /// </summary>
        /// <param name="column">Nome da coluna.</param>
        /// <returns>Valor da coluna como long nullable ou null se for NULL.</returns>
        public long? GetNullableLong(string column)
        {
            if (dr == null || string.IsNullOrEmpty(column))
                return null;

            try
            {
                int ordinal = dr.GetOrdinal(column);
                if (dr.IsDBNull(ordinal))
                    return null;

                return TryParse.ToLong(dr.GetValue(ordinal));
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Obtém o valor de uma coluna como string.
        /// </summary>
        /// <param name="column">Nome da coluna.</param>
        /// <param name="vdefault">Valor padrão se a coluna for NULL ou não encontrada.</param>
        /// <returns>Valor da coluna como string ou o valor padrão.</returns>
        public string GetString(string column, string vdefault = null)
        {
            if (dr == null || string.IsNullOrEmpty(column))
                return vdefault;

            try
            {
                int ordinal = this.dr.GetOrdinal(column);
                if (this.dr.IsDBNull(ordinal))
                {
                    return vdefault;
                }
                return this.dr.GetString(ordinal);
            }
            catch
            {
                return vdefault;
            }
        }

        /// <summary>
        /// Obtém o valor de uma coluna como string usando índice.
        /// </summary>
        /// <param name="index">Índice da coluna (baseado em zero).</param>
        /// <param name="vdefault">Valor padrão se a coluna for NULL.</param>
        /// <returns>Valor da coluna como string ou o valor padrão.</returns>
        public string GetString(int index, string vdefault = null)
        {
            if (dr == null || index < 0 || index >= dr.FieldCount)
                return vdefault;

            try
            {
                if (dr.IsDBNull(index))
                {
                    return vdefault;
                }
                return dr.GetString(index);
            }
            catch
            {
                return vdefault;
            }
        }

        /// <summary>
        /// Obtém o valor de uma coluna como string nullable.
        /// </summary>
        /// <param name="column">Nome da coluna.</param>
        /// <returns>Valor da coluna como string ou null se for NULL.</returns>
        public string GetNullableString(string column)
        {
            return GetString(column, null);
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

        /// <summary>
        /// Avança o reader para a próxima linha.
        /// </summary>
        /// <returns>True se há mais linhas, caso contrário False.</returns>
        public bool Read()
        {
            if (dr == null)
                throw new InvalidOperationException("Reader não foi inicializado.");

            try
            {
                return this.dr.Read();
            }
            catch (Exception ex)
            {
                throw new Exception($"Impossível fazer Read: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Avança o reader para a próxima linha de forma assíncrona.
        /// </summary>
        /// <returns>Task com True se há mais linhas, caso contrário False.</returns>
        public async Task<bool> ReadAsync()
        {
            if (dr == null)
                throw new InvalidOperationException("Reader não foi inicializado.");

            try
            {
                return await this.dr.ReadAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Impossível fazer ReadAsync: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Obtém todas as colunas da linha atual como um dicionário.
        /// </summary>
        /// <returns>Dicionário com nome da coluna como chave e valor como valor.</returns>
        public Dictionary<string, object> ToDictionary()
        {
            var dict = new Dictionary<string, object>();
            if (dr == null)
                return dict;

            try
            {
                for (int i = 0; i < dr.FieldCount; i++)
                {
                    string columnName = dr.GetName(i);
                    object value = dr.IsDBNull(i) ? null : dr.GetValue(i);
                    dict[columnName] = value;
                }
            }
            catch { }

            return dict;
        }

        /// <summary>
        /// Obtém todas as linhas do resultado como uma lista de dicionários.
        /// </summary>
        /// <returns>Lista de dicionários, onde cada dicionário representa uma linha.</returns>
        public List<Dictionary<string, object>> ToList()
        {
            var list = new List<Dictionary<string, object>>();
            if (dr == null)
                return list;

            try
            {
                while (dr.Read())
                {
                    var dict = new Dictionary<string, object>();
                    for (int i = 0; i < dr.FieldCount; i++)
                    {
                        string columnName = dr.GetName(i);
                        object value = dr.IsDBNull(i) ? null : dr.GetValue(i);
                        dict[columnName] = value;
                    }
                    list.Add(dict);
                }
            }
            catch { }

            return list;
        }

        /// <summary>
        /// Obtém todas as linhas do resultado como uma lista de dicionários de forma assíncrona.
        /// </summary>
        /// <returns>Task com lista de dicionários, onde cada dicionário representa uma linha.</returns>
        public async Task<List<Dictionary<string, object>>> ToListAsync()
        {
            var list = new List<Dictionary<string, object>>();
            if (dr == null)
                return list;

            try
            {
                while (await dr.ReadAsync())
                {
                    var dict = new Dictionary<string, object>();
                    for (int i = 0; i < dr.FieldCount; i++)
                    {
                        string columnName = dr.GetName(i);
                        object value = dr.IsDBNull(i) ? null : dr.GetValue(i);
                        dict[columnName] = value;
                    }
                    list.Add(dict);
                }
            }
            catch { }

            return list;
        }
    }

    public class MySQLArrayReader
    {
        public Dictionary<string, object> List = new Dictionary<string, object>();

        public MySQLArrayReader(DbDataReader dr)
        {
            if (dr != null && dr.HasRows)
            {
                for (int i = 0; i < dr.FieldCount; i++)
                {
                    string columnName = dr.GetName(i);
                    object value = dr.IsDBNull(i) ? null : dr.GetValue(i);
                    List[columnName] = value;
                }
            }
        }
    }
}