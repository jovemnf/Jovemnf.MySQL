//using MySql.Data.MySqlClient;
using MySqlConnector;
using Jovemnf.MySQL.Geometry;
using System;
using System.Collections.Generic;
using Jovemnf.DateTimeStamp;
using System.Data.Common;
using System.Threading.Tasks;
using System.Reflection;
using System.Linq;
using System.Text.Json;

namespace Jovemnf.MySQL;

public class MySQLReader(DbDataReader dr) : IDisposable, IAsyncDisposable
{
    private DbDataReader _dr = dr ?? throw new ArgumentNullException(nameof(dr));
    private readonly Dictionary<string, int> _ordinalCache = new(StringComparer.OrdinalIgnoreCase);

    private int GetOrdinal(string columnName)
    {
        if (_ordinalCache.TryGetValue(columnName, out int ordinal))
            return ordinal;

        ordinal = _dr.GetOrdinal(columnName);
        _ordinalCache[columnName] = ordinal;
        return ordinal;
    }

    /// <summary>
    /// Obtém o número de colunas no resultado.
    /// </summary>
    public int FieldCount => _dr?.FieldCount ?? 0;

    /// <summary>
    /// Indica se o resultado contém uma ou mais linhas.
    /// </summary>
    public bool HasRows => _dr?.HasRows ?? false;

    /// <summary>
    /// Verifica se uma coluna existe no resultado.
    /// </summary>
    /// <param name="columnName">Nome da coluna.</param>
    /// <returns>True se a coluna existe, caso contrário False.</returns>
    public bool ColumnExists(string columnName)
    {
        if (_dr == null || string.IsNullOrEmpty(columnName))
            return false;

        try
        {
            GetOrdinal(columnName);
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
        if (_dr == null || string.IsNullOrEmpty(column))
            return true;

        try
        {
            int ordinal = GetOrdinal(column);
            return _dr.IsDBNull(ordinal);
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
        if (_dr == null || index < 0 || index >= _dr.FieldCount)
            return true;

        try
        {
            return _dr.IsDBNull(index);
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
        if (_dr == null || index < 0 || index >= _dr.FieldCount)
            throw new ArgumentOutOfRangeException(nameof(index));

        return _dr.GetName(index);
    }

    /// <summary>
    /// Obtém uma lista com todos os nomes das colunas.
    /// </summary>
    /// <returns>Lista de nomes das colunas.</returns>
    public List<string> GetColumnNames()
    {
        var columns = new List<string>();
        if (_dr == null)
            return columns;

        try
        {
            for (int i = 0; i < _dr.FieldCount; i++)
            {
                columns.Add(_dr.GetName(i));
            }
        }
        catch { }

        return columns;
    }

    public void Dispose()
    {
        try
        {
            if (_dr != null)
            {
                _dr.Dispose();
                _dr = null;
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
            if (_dr != null)
            {
                if (_dr is IAsyncDisposable asyncDisposable)
                {
                    await asyncDisposable.DisposeAsync();
                }
                else
                {
                    _dr.Dispose();
                }
                _dr = null;
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
        if (_dr == null)
            throw new InvalidOperationException("Reader não foi inicializado.");

        if (string.IsNullOrEmpty(column))
            throw new ArgumentException("Nome da coluna não pode ser vazio.", nameof(column));

        try
        {
            int ordinal = GetOrdinal(column);
            return _dr.IsDBNull(ordinal) ? null : _dr.GetValue(ordinal);
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
        if (_dr == null)
            throw new InvalidOperationException("Reader não foi inicializado.");

        if (index < 0 || index >= _dr.FieldCount)
            throw new ArgumentOutOfRangeException(nameof(index));

        try
        {
            return _dr.IsDBNull(index) ? null : _dr.GetValue(index);
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
            aux = TryParse.ToBoolean(this._dr[column]);
        }
        catch { }
        return aux;
    }

    public bool GetBoolean(string column)
    {
        try
        {
            return TryParse.ToBoolean(this._dr[column]);
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
        if (_dr == null || string.IsNullOrEmpty(column))
            throw new InvalidOperationException("Reader não foi inicializado ou nome da coluna é inválido.");

        try
        {
            int ordinal = GetOrdinal(column);
            if (_dr.IsDBNull(ordinal))
                throw new InvalidOperationException($"A coluna '{column}' contém um valor NULL.");

            return Convert.ToDateTime(_dr.GetValue(ordinal));
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
        if (_dr == null || string.IsNullOrEmpty(column))
            return null;

        try
        {
            int ordinal = GetOrdinal(column);
            if (_dr.IsDBNull(ordinal))
                return null;

            return Convert.ToDateTime(_dr.GetValue(ordinal));
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
            return new MyDateTime( Convert.ToDateTime(this._dr[column]) );
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
            return new MyDate(Convert.ToDateTime(this._dr[column]));
        }
        catch
        {
            return new MyDate();
        }
    }

    /// <summary>
    /// Obtém o valor de uma coluna como TimeSpan.
    /// </summary>
    /// <param name="column">Nome da coluna.</param>
    /// <param name="default">Valor padrão se a coluna for NULL ou não encontrada.</param>
    /// <returns>Valor da coluna como TimeSpan ou o valor padrão.</returns>
    public TimeSpan GetTimeSpan(string column, TimeSpan @default = default)
    {
        if (_dr == null || string.IsNullOrEmpty(column))
            return @default;

        try
        {
            int ordinal = GetOrdinal(column);
            if (_dr.IsDBNull(ordinal))
                return @default;

            return TryParse.ToTimeSpan(_dr.GetValue(ordinal));
        }
        catch
        {
            return @default;
        }
    }

    /// <summary>
    /// Obtém o valor de uma coluna como TimeSpan nullable.
    /// </summary>
    /// <param name="column">Nome da coluna.</param>
    /// <returns>Valor da coluna como TimeSpan? ou null se for NULL.</returns>
    public TimeSpan? GetNullableTimeSpan(string column)
    {
        if (_dr == null || string.IsNullOrEmpty(column))
            return null;

        try
        {
            int ordinal = GetOrdinal(column);
            if (_dr.IsDBNull(ordinal))
                return null;

            return TryParse.ToTimeSpan(_dr.GetValue(ordinal));
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Obtém o valor de uma coluna como enum do tipo especificado.
    /// </summary>
    /// <typeparam name="T">Tipo do enum.</typeparam>
    /// <param name="column">Nome da coluna.</param>
    /// <returns>Valor da coluna convertido para o enum T.</returns>
    public T GetEnum<T>(string column) where T : struct, Enum
    {
        if (_dr == null || string.IsNullOrEmpty(column))
            throw new InvalidOperationException("Reader não foi inicializado ou nome da coluna é inválido.");

        try
        {
            int ordinal = GetOrdinal(column);
            if (_dr.IsDBNull(ordinal))
                throw new InvalidOperationException($"A coluna '{column}' contém um valor NULL.");

            return ParseEnum<T>(_dr.GetValue(ordinal));
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            throw new Exception($"Impossível obter enum da coluna '{column}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Obtém o valor de uma coluna como enum do tipo especificado, com valor padrão para NULL.
    /// </summary>
    /// <typeparam name="T">Tipo do enum.</typeparam>
    /// <param name="column">Nome da coluna.</param>
    /// <param name="default">Valor padrão se a coluna for NULL ou não encontrada.</param>
    /// <returns>Valor da coluna como enum ou o valor padrão.</returns>
    public T GetEnum<T>(string column, T @default) where T : struct, Enum
    {
        if (_dr == null || string.IsNullOrEmpty(column))
            return @default;

        try
        {
            int ordinal = GetOrdinal(column);
            if (_dr.IsDBNull(ordinal))
                return @default;

            return ParseEnum<T>(_dr.GetValue(ordinal));
        }
        catch
        {
            return @default;
        }
    }

    /// <summary>
    /// Obtém o valor de uma coluna como enum nullable.
    /// </summary>
    /// <typeparam name="T">Tipo do enum.</typeparam>
    /// <param name="column">Nome da coluna.</param>
    /// <returns>Valor da coluna como T? ou null se for NULL.</returns>
    public T? GetNullableEnum<T>(string column) where T : struct, Enum
    {
        if (_dr == null || string.IsNullOrEmpty(column))
            return null;

        try
        {
            int ordinal = GetOrdinal(column);
            if (_dr.IsDBNull(ordinal))
                return null;

            return ParseEnum<T>(_dr.GetValue(ordinal));
        }
        catch
        {
            return null;
        }
    }

    private static T ParseEnum<T>(object value) where T : struct, Enum
    {
        if (value == null || value == DBNull.Value)
            return default;

        if (value is T enumVal)
            return enumVal;

        var type = typeof(T);
        if (value is int i)
            return (T)Enum.ToObject(type, i);
        if (value is long l)
            return (T)Enum.ToObject(type, l);
        if (value is short s)
            return (T)Enum.ToObject(type, s);
        if (value is byte b)
            return (T)Enum.ToObject(type, b);
        if (value is decimal or double or float)
            return (T)Enum.ToObject(type, Convert.ToInt64(value));

        var str = value.ToString();
        if (string.IsNullOrEmpty(str))
            return default;

        return (T)Enum.Parse(type, str, true);
    }

    /// <summary>
    /// Obtém o valor de uma coluna como decimal.
    /// </summary>
    /// <param name="column">Nome da coluna.</param>
    /// <param name="default">Valor padrão se a coluna for NULL ou não encontrada.</param>
    /// <returns>Valor da coluna como decimal ou o valor padrão.</returns>
    public decimal GetDecimal(string column, decimal @default = 0)
    {
        if (_dr == null || string.IsNullOrEmpty(column))
            return @default;

        try
        {
            int ordinal = GetOrdinal(column);
            if (_dr.IsDBNull(ordinal))
                return @default;

            return TryParse.ToDecimal(_dr.GetValue(ordinal));
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
        if (_dr == null || string.IsNullOrEmpty(column))
            return null;

        try
        {
            int ordinal = GetOrdinal(column);
            if (_dr.IsDBNull(ordinal))
                return null;

            return TryParse.ToDecimal(_dr.GetValue(ordinal));
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
        if (_dr == null || string.IsNullOrEmpty(column))
            return @default;

        try
        {
            int ordinal = GetOrdinal(column);
            if (_dr.IsDBNull(ordinal))
                return @default;

            return TryParse.ToDouble(_dr.GetValue(ordinal));
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
        if (_dr == null || string.IsNullOrEmpty(column))
            return null;

        try
        {
            int ordinal = GetOrdinal(column);
            if (_dr.IsDBNull(ordinal))
                return null;

            return TryParse.ToDouble(_dr.GetValue(ordinal));
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
            return new MySQLArrayReader(_dr);
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
        if (_dr == null || string.IsNullOrEmpty(column))
            return @default;

        try
        {
            int ordinal = GetOrdinal(column);
            if (_dr.IsDBNull(ordinal))
                return @default;

            return TryParse.ToInt32(_dr.GetValue(ordinal));
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
        if (_dr == null || index < 0 || index >= _dr.FieldCount)
            return @default;

        try
        {
            if (_dr.IsDBNull(index))
                return @default;

            return TryParse.ToInt32(_dr.GetValue(index));
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
        if (_dr == null || string.IsNullOrEmpty(column))
            return null;

        try
        {
            int ordinal = GetOrdinal(column);
            if (_dr.IsDBNull(ordinal))
                return null;

            return TryParse.ToInt32(_dr.GetValue(ordinal));
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
        if (_dr == null || string.IsNullOrEmpty(column))
            return @default;

        try
        {
            int ordinal = GetOrdinal(column);
            if (_dr.IsDBNull(ordinal))
                return @default;

            return TryParse.ToLong(_dr.GetValue(ordinal));
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
        if (_dr == null || index < 0 || index >= _dr.FieldCount)
            return @default;

        try
        {
            if (_dr.IsDBNull(index))
                return @default;

            return TryParse.ToLong(_dr.GetValue(index));
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
        if (_dr == null || string.IsNullOrEmpty(column))
            return null;

        try
        {
            int ordinal = GetOrdinal(column);
            if (_dr.IsDBNull(ordinal))
                return null;

            return TryParse.ToLong(_dr.GetValue(ordinal));
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
        if (_dr == null || string.IsNullOrEmpty(column))
            return vdefault;

        try
        {
            int ordinal = GetOrdinal(column);
            if (this._dr.IsDBNull(ordinal))
            {
                return vdefault;
            }
            return this._dr.GetString(ordinal);
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
        if (_dr == null || index < 0 || index >= _dr.FieldCount)
            return vdefault;

        try
        {
            if (_dr.IsDBNull(index))
            {
                return vdefault;
            }
            return _dr.GetString(index);
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
            CS10000 = (byte[]) this._dr[column];
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
        if (_dr == null)
            throw new InvalidOperationException("Reader não foi inicializado.");

        try
        {
            return this._dr.Read();
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
        if (_dr == null)
            throw new InvalidOperationException("Reader não foi inicializado.");

        try
        {
            return await this._dr.ReadAsync();
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
        if (_dr == null)
            return dict;

        try
        {
            for (int i = 0; i < _dr.FieldCount; i++)
            {
                string columnName = _dr.GetName(i);
                object value = _dr.IsDBNull(i) ? null : _dr.GetValue(i);
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
        if (_dr == null)
            return list;

        try
        {
            while (_dr.Read())
            {
                var dict = new Dictionary<string, object>();
                for (int i = 0; i < _dr.FieldCount; i++)
                {
                    string columnName = _dr.GetName(i);
                    object value = _dr.IsDBNull(i) ? null : _dr.GetValue(i);
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
        if (_dr == null)
            return list;

        try
        {
            while (await _dr.ReadAsync())
            {
                var dict = new Dictionary<string, object>();
                for (int i = 0; i < _dr.FieldCount; i++)
                {
                    string columnName = _dr.GetName(i);
                    object value = _dr.IsDBNull(i) ? null : _dr.GetValue(i);
                    dict[columnName] = value;
                }
                list.Add(dict);
            }
        }
        catch { }

        return list;
    }

    /// <summary>
    /// Mapeia a linha atual para um modelo do tipo T.
    /// </summary>
    /// <typeparam name="T">O tipo do modelo a ser preenchido.</typeparam>
    /// <returns>Uma instância de T preenchida com os dados da linha atual.</returns>
    public T ToModel<T>() where T : new()
    {
        var model = new T();
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var columns = GetColumnNames();

        foreach (var prop in properties)
        {
            // Check for DbField attribute
            var dbFieldAttr = prop.GetCustomAttribute<DbFieldAttribute>(inherit: true);
            string targetColumnName = dbFieldAttr?.Name ?? prop.Name;

            // Try exact match or case-insensitive match
            string columnName = columns.FirstOrDefault(c => 
                string.Equals(c, targetColumnName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(c.Replace("_", ""), targetColumnName, StringComparison.OrdinalIgnoreCase));
                
            if (columnName != null)
            {
                object val = Get(columnName);
                if (val != null && val != DBNull.Value)
                {
                    try
                    {
                        Type propType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                            
                        object convertedValue = TryParse.ChangeType(val, propType);
                            
                        if (convertedValue != null)
                        {
                            prop.SetValue(model, convertedValue);
                        }
                        else if (propType.IsEnum)
                        {
                            prop.SetValue(model, Enum.Parse(propType, val.ToString()));
                        }
                        else if (val is string jsonString && IsJsonString(jsonString))
                        {
                            // Automatic JSON deserialization for complex types and custom date types
                            try
                            {
                                if (propType == typeof(MyDateTime))
                                {
                                    var dateTime = JsonSerializer.Deserialize<DateTime>(jsonString);
                                    prop.SetValue(model, new MyDateTime(dateTime));
                                }
                                else if (propType == typeof(MyDate))
                                {
                                    var dateTime = JsonSerializer.Deserialize<DateTime>(jsonString);
                                    prop.SetValue(model, new MyDate(dateTime));
                                }
                                else if (IsComplexType(propType))
                                {
                                    // Use case-insensitive property matching for JSON deserialization
                                    var options = new JsonSerializerOptions
                                    {
                                        PropertyNameCaseInsensitive = true,
                                        PropertyNamingPolicy = null
                                    };
                                    var deserializedValue = JsonSerializer.Deserialize(jsonString, propType, options);
                                    prop.SetValue(model, deserializedValue);
                                }
                            }
                            catch (Exception)
                            {
                                // If JSON deserialization fails, try standard conversion
                                try
                                {
                                    if (propType == typeof(MyDateTime))
                                    {
                                        prop.SetValue(model, new MyDateTime(Convert.ToDateTime(val)));
                                    }
                                    else if (propType == typeof(MyDate))
                                    {
                                        prop.SetValue(model, new MyDate(Convert.ToDateTime(val)));
                                    }
                                    else
                                    {
                                        prop.SetValue(model, Convert.ChangeType(val, propType));
                                    }
                                }
                                catch 
                                { 
                                    // Silently ignore if both JSON and standard conversion fail
                                    // This allows partial object mapping to succeed
                                }
                            }
                        }
                        else if (propType == typeof(Point))
                        {
                            // Handle GEOMETRY POINT type
                            if (val is byte[] wkb)
                            {
                                var point = Point.FromWKB(wkb);
                                prop.SetValue(model, point);
                            }
                        }
                        else if (propType == typeof(Polygon))
                        {
                            // Handle GEOMETRY POLYGON type
                            // Note: SELECT should use ST_AsText(polygon_column)
                            if (val is string wkt)
                            {
                                var polygon = Polygon.FromWKT(wkt);
                                prop.SetValue(model, polygon);
                            }
                        }
                        else if (propType == typeof(MyDateTime))
                        {
                            prop.SetValue(model, new MyDateTime(Convert.ToDateTime(val)));
                        }
                        else if (propType == typeof(MyDate))
                        {
                            prop.SetValue(model, new MyDate(Convert.ToDateTime(val)));
                        }
                        else
                        {
                            prop.SetValue(model, Convert.ChangeType(val, propType));
                        }
                    }
                    catch
                    {
                        // Ignore mapping errors for individual properties
                    }
                }
            }
        }

        return model;
    }

    /// <summary>
    /// Verifica se um tipo é complexo (não é um tipo primitivo ou string).
    /// </summary>
    /// <param name="type">O tipo a ser verificado.</param>
    /// <returns>True se o tipo é complexo, caso contrário False.</returns>
    private bool IsComplexType(Type type)
    {
        return !type.IsPrimitive 
               && type != typeof(string) 
               && type != typeof(decimal) 
               && type != typeof(DateTime) 
               && type != typeof(TimeSpan)
               && type != typeof(MyDateTime)
               && type != typeof(MyDate)
               && !type.IsEnum;
    }

    /// <summary>
    /// Verifica se uma string é um JSON válido.
    /// </summary>
    /// <param name="str">A string a ser verificada.</param>
    /// <returns>True se a string é um JSON válido, caso contrário False.</returns>
    private bool IsJsonString(string str)
    {
        if (string.IsNullOrWhiteSpace(str))
            return false;

        str = str.Trim();
        return (str.StartsWith("{") && str.EndsWith("}")) 
               || (str.StartsWith("[") && str.EndsWith("]"));
    }

    /// <summary>
    /// Mapeia todas as linhas do resultado para uma lista de modelos do tipo T.
    /// </summary>
    /// <typeparam name="T">O tipo do modelo.</typeparam>
    /// <returns>Lista de instâncias de T.</returns>
    public List<T> ToModelList<T>() where T : new()
    {
        var list = new List<T>();
        try
        {
            while (Read())
            {
                list.Add(ToModel<T>());
            }
        }
        catch
        {
            // ignored
        }

        return list;
    }

    /// <summary>
    /// Mapeia todas as linhas do resultado para uma lista de modelos do tipo T de forma assíncrona.
    /// </summary>
    /// <typeparam name="T">O tipo do modelo.</typeparam>
    /// <returns>Task com lista de instâncias de T.</returns>
    public async Task<List<T>> ToModelListAsync<T>() where T : new()
    {
        var list = new List<T>();
        try
        {
            while (await ReadAsync())
            {
                list.Add(ToModel<T>());
            }
        }
        catch
        {
            // ignored
        }

        return list;
    }
}

public class MySQLArrayReader
{
    private readonly Dictionary<string, object> _list = new();

    public MySQLArrayReader(DbDataReader dr)
    {
        if (dr == null || !dr.HasRows) return;
        for (var i = 0; i < dr.FieldCount; i++)
        {
            var columnName = dr.GetName(i);
            var value = dr.IsDBNull(i) ? null : dr.GetValue(i);
            _list[columnName] = value;
        }
    }
}