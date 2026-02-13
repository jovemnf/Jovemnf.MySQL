using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Reflection;
using MySqlConnector;
using Jovemnf.MySQL.Geometry;

namespace Jovemnf.MySQL.Builder;

public class InsertQueryBuilder
{
    protected string _tableName;
    private Dictionary<string, object> _fields = new Dictionary<string, object>();
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
        var columnNames = new List<string>();
        var paramNames = new List<string>();

        foreach (var field in _fields)
        {
            var paramName = $"p{_paramCounter++}";
            columnNames.Add($"`{EscapeIdentifier(field.Key)}`");
            
            if (field.Value is PointValue pointValue)
            {
                // Use ST_GeomFromWKB for POINT values
                paramNames.Add($"ST_GeomFromWKB(@{paramName}, {pointValue.Point.SRID})");
                command.Parameters.AddWithValue($"@{paramName}", pointValue.Point.ToWKB());
            }
            else if (field.Value is PolygonValue polygonValue)
            {
                // Use ST_PolygonFromText for POLYGON values
                paramNames.Add($"ST_PolygonFromText(@{paramName}, {polygonValue.Polygon.SRID})");
                command.Parameters.AddWithValue($"@{paramName}", polygonValue.Polygon.ToWKT());
            }
            else
            {
                paramNames.Add($"@{paramName}");
                command.Parameters.AddWithValue($"@{paramName}", field.Value ?? DBNull.Value);
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

    // Helper class to mark Point values for special handling
    private class PointValue
    {
        public Point Point { get; }
        public PointValue(Point point) => Point = point;
    }

    // Helper class to mark Polygon values for special handling
    private class PolygonValue
    {
        public Polygon Polygon { get; }
        public PolygonValue(Polygon polygon) => Polygon = polygon;
    }
}

public class InsertQueryBuilder<T> : InsertQueryBuilder
{
    private static readonly Dictionary<string, string> _fieldMapping = CreateFieldMapping();

    private static Dictionary<string, string> CreateFieldMapping()
    {
        var mapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in typeof(T).GetProperties())
        {
            var attr = p.GetCustomAttribute<DbFieldAttribute>(true);
            var columnName = attr?.Name ?? p.Name;
            
            mapping[p.Name] = columnName;
            mapping[p.Name.ToSnakeCase()] = columnName;
        }
        return mapping;
    }

    public InsertQueryBuilder()
    {
        Table(GetTableName<T>());
    }

    protected override string ResolveField(string field)
    {
        return _fieldMapping.TryGetValue(field, out var column) ? column : field;
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
        var (sql, command) = builder.Build();
        command.Connection = _connection;
        
        if (_transaction != null)
            command.Transaction = _transaction;

        await command.ExecuteNonQueryAsync();

        if (lastID)
        {
            using (var idCmd = new MySqlCommand("SELECT LAST_INSERT_ID()", _connection))
            {
                if (_transaction != null) idCmd.Transaction = _transaction;
                return Convert.ToInt64(await idCmd.ExecuteScalarAsync());
            }
        }
        return 0;
    }

    public long Execute(InsertQueryBuilder builder, bool lastID = true)
    {
        var (sql, command) = builder.Build();
        command.Connection = _connection;
        
        if (_transaction != null)
            command.Transaction = _transaction;

        command.ExecuteNonQuery();

        if (lastID)
        {
            using (var idCmd = new MySqlCommand("SELECT LAST_INSERT_ID()", _connection))
            {
                if (_transaction != null) idCmd.Transaction = _transaction;
                return Convert.ToInt64(idCmd.ExecuteScalar());
            }
        }
        return 0;
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
