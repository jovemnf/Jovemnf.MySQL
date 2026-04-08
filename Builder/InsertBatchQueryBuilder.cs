using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Jovemnf.MySQL.Geometry;
using MySqlConnector;

namespace Jovemnf.MySQL.Builder;

public class InsertBatchQueryBuilder
{
    private string _tableName;
    private readonly List<Dictionary<string, object>> _rows = [];
    private readonly List<string> _columnOrder = [];
    private readonly HashSet<string> _columnSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _duplicateUpdateFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _duplicateExcludedFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private bool _updateAllExcept;
    private int _paramCounter = 0;

    public static InsertBatchQueryBuilder<T> For<T>() => new InsertBatchQueryBuilder<T>();

    protected virtual string ResolveField(string field) => field;

    private string EscapeIdentifier(string identifier)
    {
        if (string.IsNullOrEmpty(identifier)) return identifier;
        return identifier.Replace("`", "``");
    }

    public InsertBatchQueryBuilder Table(string tableName)
    {
        _tableName = tableName;
        return this;
    }

    public InsertBatchQueryBuilder Row(Dictionary<string, object> fields)
    {
        ArgumentNullException.ThrowIfNull(fields);
        if (fields.Count == 0)
            throw new InvalidOperationException("A linha do batch não pode estar vazia.");

        var row = new Dictionary<string, object>(fields.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var field in fields)
        {
            var resolvedField = ResolveField(field.Key);
            row[resolvedField] = NormalizeValue(field.Value);
        }

        RegisterRow(row);
        return this;
    }

    public InsertBatchQueryBuilder Rows(IEnumerable<Dictionary<string, object>> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);

        foreach (var row in rows)
        {
            Row(row);
        }

        return this;
    }

    public InsertBatchQueryBuilder Rows<TItem>(
        IEnumerable<TItem> items,
        Func<TItem, Dictionary<string, object>> map)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(map);

        foreach (var item in items)
        {
            Row(map(item));
        }

        return this;
    }

    public InsertBatchQueryBuilder OnDuplicateKeyUpdate(params string[] fields)
    {
        ArgumentNullException.ThrowIfNull(fields);
        if (fields.Length == 0)
            throw new InvalidOperationException("Informe ao menos um campo para atualizar em caso de chave duplicada.");

        _updateAllExcept = false;
        _duplicateUpdateFields.Clear();
        _duplicateExcludedFields.Clear();

        foreach (var field in fields)
        {
            _duplicateUpdateFields.Add(ResolveField(field));
        }

        return this;
    }

    public InsertBatchQueryBuilder OnDuplicateKeyUpdateAllExcept(params string[] excludedFields)
    {
        _updateAllExcept = true;
        _duplicateUpdateFields.Clear();
        _duplicateExcludedFields.Clear();

        if (excludedFields != null)
        {
            foreach (var field in excludedFields)
            {
                _duplicateExcludedFields.Add(ResolveField(field));
            }
        }

        return this;
    }

    public Task<int> ExecuteAsync(MySQL connection)
    {
        ArgumentNullException.ThrowIfNull(connection);
        if (_tableName == null)
            throw new InvalidOperationException("Tabela não especificada");
        if (_rows.Count == 0)
            throw new InvalidOperationException("Nenhuma linha para inserir");

        return connection.ExecuteInsertBatchAsync(this);
    }

    public (string Sql, MySqlCommand Command) Build()
    {
        _paramCounter = 0;
        if (string.IsNullOrEmpty(_tableName))
            throw new InvalidOperationException("Nome da tabela não definido");
        if (_rows.Count == 0)
            throw new InvalidOperationException("Nenhuma linha para inserir");

        var command = new MySqlCommand();
        var columnNames = new List<string>(_columnOrder.Count);
        foreach (var column in _columnOrder)
        {
            columnNames.Add($"`{EscapeIdentifier(column)}`");
        }

        var valueRows = new List<string>(_rows.Count);
        foreach (var row in _rows)
        {
            var rowValues = new List<string>(_columnOrder.Count);
            foreach (var column in _columnOrder)
            {
                var paramName = GetNextParamName();
                var value = row[column];

                if (value is PointValue pointValue)
                {
                    rowValues.Add($"ST_GeomFromWKB(@{paramName}, {pointValue.Point.SRID})");
                    AddParameter(command, paramName, pointValue.Point.ToWKB());
                }
                else if (value is PolygonValue polygonValue)
                {
                    rowValues.Add($"ST_PolygonFromText(@{paramName}, {polygonValue.Polygon.SRID})");
                    AddParameter(command, paramName, polygonValue.Polygon.ToWKT());
                }
                else
                {
                    rowValues.Add($"@{paramName}");
                    AddParameter(command, paramName, value);
                }
            }

            valueRows.Add($"({string.Join(", ", rowValues)})");
        }

        var sql = $"INSERT INTO `{EscapeIdentifier(_tableName)}` ({string.Join(", ", columnNames)}) VALUES {string.Join(", ", valueRows)}";
        var updateFields = ResolveDuplicateUpdateFields();
        if (updateFields.Count > 0)
        {
            var updates = new List<string>(updateFields.Count);
            foreach (var field in updateFields)
            {
                var escapedField = EscapeIdentifier(field);
                updates.Add($"`{escapedField}` = VALUES(`{escapedField}`)");
            }

            sql += $" ON DUPLICATE KEY UPDATE {string.Join(", ", updates)}";
        }

        command.CommandText = sql;
        return (sql, command);
    }

    public override string ToString()
    {
        return Build().Sql;
    }

    private List<string> ResolveDuplicateUpdateFields()
    {
        if (_updateAllExcept)
        {
            var fields = new List<string>(_columnOrder.Count);
            foreach (var column in _columnOrder)
            {
                if (!_duplicateExcludedFields.Contains(column))
                    fields.Add(column);
            }

            if (fields.Count == 0)
                throw new InvalidOperationException("Nenhum campo restou para atualizar em caso de chave duplicada.");

            return fields;
        }

        if (_duplicateUpdateFields.Count == 0)
            return new List<string>();

        var result = new List<string>(_duplicateUpdateFields.Count);
        foreach (var field in _duplicateUpdateFields)
        {
            if (!_columnSet.Contains(field))
                throw new InvalidOperationException($"O campo '{field}' não existe nas linhas do batch.");

            result.Add(field);
        }

        return result;
    }

    private void RegisterRow(Dictionary<string, object> row)
    {
        if (_rows.Count == 0)
        {
            foreach (var column in row.Keys)
            {
                _columnOrder.Add(column);
                _columnSet.Add(column);
            }

            _rows.Add(row);
            return;
        }

        if (row.Count != _columnOrder.Count)
            throw new InvalidOperationException("Todas as linhas do batch devem ter a mesma quantidade de colunas.");

        foreach (var column in _columnOrder)
        {
            if (!row.ContainsKey(column))
                throw new InvalidOperationException("Todas as linhas do batch devem possuir o mesmo conjunto de colunas.");
        }

        _rows.Add(row);
    }

    private object NormalizeValue(object value)
    {
        if (value is Point point)
            return new PointValue(point);
        if (value is Polygon polygon)
            return new PolygonValue(polygon);
        return value;
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

    private class PointValue
    {
        public Point Point { get; }
        public PointValue(Point point) => Point = point;
    }

    private class PolygonValue(Polygon polygon)
    {
        public Polygon Polygon { get; } = polygon;
    }
}

public class InsertBatchQueryBuilder<T> : InsertBatchQueryBuilder
{
    private static readonly Dictionary<string, string> FieldMapping = CreateFieldMapping();
    private static readonly string ResolvedTableName = GetTableName<T>();
    private static readonly PropertyColumnMapping[] InsertableProperties = CreateInsertableProperties();

    private static Dictionary<string, string> CreateFieldMapping()
    {
        var mapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in typeof(T).GetProperties())
        {
            var attr = property.GetCustomAttribute<DbFieldAttribute>(true);
            var columnName = attr?.Name ?? property.Name.ToSnakeCase();
            mapping[property.Name] = columnName;
            mapping[property.Name.ToSnakeCase()] = columnName;
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

    protected static string GetTableName<TModel>()
    {
        var attr = (DbTableAttribute)Attribute.GetCustomAttribute(typeof(TModel), typeof(DbTableAttribute));
        return attr?.Name ?? typeof(TModel).Name;
    }

    public InsertBatchQueryBuilder()
    {
        Table(ResolvedTableName);
    }

    public InsertBatchQueryBuilder<T> RowFrom(T entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));

        var row = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        foreach (var mapping in InsertableProperties)
        {
            var value = mapping.Property.GetValue(entity);
            if (value == null) continue;

            row[mapping.ColumnName] = value;
        }

        Row(row);
        return this;
    }

    public InsertBatchQueryBuilder<T> RowsFrom(IEnumerable<T> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);

        foreach (var entity in entities)
        {
            RowFrom(entity);
        }

        return this;
    }

    public InsertBatchQueryBuilder<T> RowAsJson(string field, object value)
    {
        Row(new Dictionary<string, object>
        {
            [field] = value == null ? DBNull.Value : JsonSerializer.Serialize(value)
        });
        return this;
    }

    protected override string ResolveField(string field)
    {
        if (FieldMapping.TryGetValue(field, out var columnName))
            return columnName;

        return field.ToSnakeCase();
    }

    private sealed class PropertyColumnMapping
    {
        public PropertyInfo Property { get; }
        public string ColumnName { get; }

        public PropertyColumnMapping(PropertyInfo property, string columnName)
        {
            Property = property;
            ColumnName = columnName;
        }
    }
}

public class InsertBatchQueryExecutor
{
    private readonly MySqlConnection _connection;
    private readonly MySqlTransaction _transaction;

    public InsertBatchQueryExecutor(MySqlConnection connection)
    {
        _connection = connection;
    }

    public InsertBatchQueryExecutor(MySqlConnection connection, MySqlTransaction transaction)
    {
        _connection = connection;
        _transaction = transaction;
    }

    public async Task<int> ExecuteAsync(InsertBatchQueryBuilder builder)
    {
        var (_, command) = builder.Build();
        using (command)
        {
            command.Connection = _connection;

            if (_transaction != null)
                command.Transaction = _transaction;

            return await command.ExecuteNonQueryAsync();
        }
    }

    public int Execute(InsertBatchQueryBuilder builder)
    {
        var (_, command) = builder.Build();
        using (command)
        {
            command.Connection = _connection;

            if (_transaction != null)
                command.Transaction = _transaction;

            return command.ExecuteNonQuery();
        }
    }
}
