using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Jovemnf.MySQL.Geometry;
using MySqlConnector;

namespace Jovemnf.MySQL.Builder;

public class InsertQueryBuilder
{
    private string _tableName;
    private readonly Dictionary<string, object> _fields = new();
    private int _paramCounter = 0;

    public static InsertQueryBuilder For<T>() => new InsertQueryBuilder<T>();

    protected virtual string ResolveField(string field) => field;

    private string EscapeIdentifier(string identifier)
    {
        if (string.IsNullOrEmpty(identifier)) return identifier;
        return identifier.Replace("`", "``");
    }

    public InsertQueryBuilder Table(string tableName)
    {
        _tableName = tableName;
        return this;
    }

    public InsertQueryBuilder Value(string field, object value)
    {
        field = ResolveField(field);
        // Auto-detect GEOMETRY types
        if (value is Point point)
        {
            _fields[field] = new PointValue(point);
        }
        else if (value is Polygon polygon)
        {
            _fields[field] = new PolygonValue(polygon);
        }
        else
        {
            _fields[field] = value;
        }
        return this;
    }
    
    public Task ExecuteAsync(MySQL connection)
    {
        ArgumentNullException.ThrowIfNull(connection);
        if (_tableName == null) 
            throw new InvalidOperationException("Tabela não especificada");
        if (_fields.Count == 0) 
            throw new InvalidOperationException("Nenhum campo para inserir");

        return connection.ExecuteInsertAsync(this);
    }

    /// <summary>
    /// Executa o INSERT e retorna a linha inserida mapeada para o tipo T (via LAST_INSERT_ID + SELECT).
    /// Assume que a tabela possui coluna de chave primária "id" auto-incremento.
    /// </summary>
    /// <typeparam name="T">Tipo do modelo (deve ter construtor sem parâmetros e propriedades mapeáveis).</typeparam>
    /// <param name="connection">Conexão MySQL.</param>
    /// <returns>A entidade inserida com dados do banco (incluindo id gerado) ou default se falhar.</returns>
    public Task<T> ExecuteAsync<T>(MySQL connection) where T : new()
    {
        ArgumentNullException.ThrowIfNull(connection);
        if (_tableName == null) 
            throw new InvalidOperationException("Tabela não especificada");
        if (_fields.Count == 0) 
            throw new InvalidOperationException("Nenhum campo para inserir");

        return connection.ExecuteInsertAsync<T>(this);
    }

    /// <summary>
    /// Constrói SELECT * para a linha inserida por id (usado por ExecuteAsync&lt;T&gt;).
    /// </summary>
    internal (string Sql, MySqlCommand Command) BuildSelectById(long id)
    {
        if (string.IsNullOrEmpty(_tableName))
            throw new InvalidOperationException("Tabela não especificada");
        var command = new MySqlCommand();
        AddParameter(command, "p0", id);
        var sql = $"SELECT * FROM `{EscapeIdentifier(_tableName)}` WHERE `id` = @p0";
        command.CommandText = sql;
        return (sql, command);
    }

    public InsertQueryBuilder Values(Dictionary<string, object> fields)
    {
        foreach (var field in fields)
        {
            _fields[ResolveField(field.Key)] = field.Value;
        }
        return this;
    }

    /// <summary>
    /// Adiciona um campo com valor serializado como JSON.
    /// </summary>
    /// <param name="field">Nome do campo.</param>
    /// <param name="value">Objeto a ser serializado para JSON.</param>
    /// <returns>O builder para encadeamento.</returns>
    public InsertQueryBuilder ValueAsJson(string field, object value)
    {
        field = ResolveField(field);
        if (value == null)
        {
            _fields[field] = DBNull.Value;
        }
        else
        {
            var json = JsonSerializer.Serialize(value);
            _fields[field] = json;
        }
        return this;
    }

    /// <summary>
    /// Adiciona um campo com valor serializado como JSON usando opções customizadas.
    /// </summary>
    /// <param name="field">Nome do campo.</param>
    /// <param name="value">Objeto a ser serializado para JSON.</param>
    /// <param name="options">Opções de serialização JSON.</param>
    /// <returns>O builder para encadeamento.</returns>
    public InsertQueryBuilder ValueAsJson(string field, object value, JsonSerializerOptions options)
    {
        field = ResolveField(field);
        if (value == null)
        {
            _fields[field] = DBNull.Value;
        }
        else
        {
            var json = JsonSerializer.Serialize(value, options);
            _fields[field] = json;
        }
        return this;
    }

    /// <summary>
    /// Adiciona um campo GEOMETRY POINT usando ST_GeomFromWKB.
    /// </summary>
    /// <param name="field">Nome do campo.</param>
    /// <param name="point">Objeto Point a ser inserido.</param>
    /// <returns>O builder para encadeamento.</returns>
    public InsertQueryBuilder ValueAsPoint(string field, Point point)
    {
        field = ResolveField(field);
        if (point == null)
        {
            _fields[field] = DBNull.Value;
        }
        else
        {
            _fields[field] = new PointValue(point);
        }
        return this;
    }

    /// <summary>
    /// Adiciona um campo GEOMETRY POLYGON usando ST_PolygonFromText.
    /// </summary>
    /// <param name="field">Nome do campo.</param>
    /// <param name="polygon">Objeto Polygon a ser inserido.</param>
    /// <returns>O builder para encadeamento.</returns>
    public InsertQueryBuilder ValueAsPolygon(string field, Polygon polygon)
    {
        field = ResolveField(field);
        if (polygon == null)
        {
            _fields[field] = DBNull.Value;
        }
        else
        {
            _fields[field] = new PolygonValue(polygon);
        }
        return this;
    }

    public (string Sql, MySqlCommand Command) Build()
    {
        _paramCounter = 0;
        if (string.IsNullOrEmpty(_tableName))
            throw new InvalidOperationException("Nome da tabela não definido");
        
        if (_fields.Count == 0)
            throw new InvalidOperationException("Nenhum campo para inserir");

        var command = new MySqlCommand();
        var columnNames = new List<string>(_fields.Count);
        var paramNames = new List<string>(_fields.Count);

        foreach (var field in _fields)
        {
            var paramName = GetNextParamName();
            var escapedField = EscapeIdentifier(field.Key);
            columnNames.Add($"`{escapedField}`");
            
            if (field.Value is PointValue pointValue)
            {
                // Use ST_GeomFromWKB for POINT values
                paramNames.Add($"ST_GeomFromWKB(@{paramName}, {pointValue.Point.SRID})");
                AddParameter(command, paramName, pointValue.Point.ToWKB());
            }
            else if (field.Value is PolygonValue polygonValue)
            {
                // Use ST_PolygonFromText for POLYGON values
                paramNames.Add($"ST_PolygonFromText(@{paramName}, {polygonValue.Polygon.SRID})");
                AddParameter(command, paramName, polygonValue.Polygon.ToWKT());
            }
            else
            {
                paramNames.Add($"@{paramName}");
                AddParameter(command, paramName, field.Value);
            }
        }

        var sql = $"INSERT INTO `{EscapeIdentifier(_tableName)}` ({string.Join(", ", columnNames)}) VALUES ({string.Join(", ", paramNames)})";
        command.CommandText = sql;
        
        return (sql, command);
    }

    protected static string GetTableName<T>()
    {
        var attr = (DbTableAttribute)Attribute.GetCustomAttribute(typeof(T), typeof(DbTableAttribute));
        return attr?.Name ?? typeof(T).Name;
    }

    public override string ToString()
    {
        return Build().Sql;
    }

    private string GetNextParamName() => $"p{_paramCounter++}";

    private static void AddParameter(MySqlCommand command, string paramName, object value)
    {
        var parameter = new MySqlParameter($"@{paramName}", value ?? DBNull.Value);

        switch (value)
        {
            case string:
                parameter.DbType = DbType.String;
                break;
            case byte[]:
                parameter.DbType = DbType.Binary;
                break;
            case bool:
                parameter.DbType = DbType.Boolean;
                break;
            case byte:
                parameter.DbType = DbType.Byte;
                break;
            case sbyte:
                parameter.DbType = DbType.SByte;
                break;
            case short:
                parameter.DbType = DbType.Int16;
                break;
            case ushort:
                parameter.DbType = DbType.UInt16;
                break;
            case int:
                parameter.DbType = DbType.Int32;
                break;
            case uint:
                parameter.DbType = DbType.UInt32;
                break;
            case long:
                parameter.DbType = DbType.Int64;
                break;
            case ulong:
                parameter.DbType = DbType.UInt64;
                break;
            case float:
                parameter.DbType = DbType.Single;
                break;
            case double:
                parameter.DbType = DbType.Double;
                break;
            case decimal:
                parameter.DbType = DbType.Decimal;
                break;
            case DateTime:
                parameter.DbType = DbType.DateTime;
                break;
            case Guid:
                parameter.DbType = DbType.Guid;
                break;
        }

        command.Parameters.Add(parameter);
    }

    // Helper class to mark Point values for special handling
    private class PointValue(Point point)
    {
        public Point Point { get; } = point;
    }

    // Helper class to mark Polygon values for special handling
    private class PolygonValue(Polygon polygon)
    {
        public Polygon Polygon { get; } = polygon;
    }
}

public class InsertQueryBuilder<T> : InsertQueryBuilder
{
    private static readonly Dictionary<string, string> FieldMapping = CreateFieldMapping();
    private static readonly string ResolvedTableName = GetTableName<T>();
    private static readonly PropertyColumnMapping[] InsertableProperties = CreateInsertableProperties();

    private static Dictionary<string, string> CreateFieldMapping()
    {
        var mapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in typeof(T).GetProperties())
        {
            var attr = p.GetCustomAttribute<DbFieldAttribute>(true);
            var columnName = attr?.Name ?? p.Name.ToSnakeCase();
            
            mapping[p.Name] = columnName;
            mapping[p.Name.ToSnakeCase()] = columnName;
        }
        return mapping;
    }

    private static PropertyColumnMapping[] CreateInsertableProperties()
    {
        var properties = typeof(T).GetProperties();
        var mappings = new List<PropertyColumnMapping>(properties.Length);

        foreach (var property in properties)
        {
            if (property.GetCustomAttribute<IgnoreToDictionaryAttribute>(true) != null)
                continue;

            var attr = property.GetCustomAttribute<DbFieldAttribute>(true);
            var columnName = attr?.Name ?? property.Name.ToSnakeCase();
            mappings.Add(new PropertyColumnMapping(property, columnName));
        }

        return mappings.ToArray();
    }

    public InsertQueryBuilder()
    {
        Table(ResolvedTableName);
    }

    public InsertQueryBuilder<T> ValuesFrom(T entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));

        foreach (var mapping in InsertableProperties)
        {
            var value = mapping.Property.GetValue(entity);
            if (value == null) continue;

            Value(mapping.ColumnName, value);
        }
        return this;
    }

    protected override string ResolveField(string field)
    {
        if (FieldMapping.TryGetValue(field, out var columnName))
            return columnName;
            
        return field.ToSnakeCase();
    }

    private sealed class PropertyColumnMapping(PropertyInfo property, string columnName)
    {
        public PropertyInfo Property { get; } = property;
        public string ColumnName { get; } = columnName;
    }
}

public class InsertQueryExecutor
{
    private readonly MySqlConnection _connection;
    private MySqlTransaction _transaction;

    public InsertQueryExecutor(MySqlConnection connection)
    {
        _connection = connection;
    }

    public InsertQueryExecutor(MySqlConnection connection, MySqlTransaction transaction)
    {
        _connection = connection;
        _transaction = transaction;
    }

    public async Task<long> ExecuteAsync(InsertQueryBuilder builder, bool lastID = true)
    {
        var (_, command) = builder.Build();
        await using (command)
        {
            command.Connection = _connection;
            
            if (_transaction != null)
                command.Transaction = _transaction;

            await command.ExecuteNonQueryAsync();
        }

        if (!lastID) return 0;
        await using (var idCmd = new MySqlCommand("SELECT LAST_INSERT_ID()", _connection))
        {
            if (_transaction != null) idCmd.Transaction = _transaction;
            return Convert.ToInt64(await idCmd.ExecuteScalarAsync());
        }
    }

    public long Execute(InsertQueryBuilder builder, bool lastID = true)
    {
        var (_, command) = builder.Build();
        using (command)
        {
            command.Connection = _connection;
            
            if (_transaction != null)
                command.Transaction = _transaction;

            command.ExecuteNonQuery();
        }

        if (!lastID) return 0;
        using var idCmd = new MySqlCommand("SELECT LAST_INSERT_ID()", _connection);
        if (_transaction != null) idCmd.Transaction = _transaction;
        return Convert.ToInt64(idCmd.ExecuteScalar());
    }
}

public class InsertResult
{
    public bool Success { get; set; }
    public long LastInsertedId { get; set; }
    public string Sql { get; set; }
    public TimeSpan ExecutionTime { get; set; }
    public string Error { get; set; }
    public Exception Exception { get; set; }
}
