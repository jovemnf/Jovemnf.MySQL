using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Jovemnf.MySQL.Geometry;
using MySqlConnector;

namespace Jovemnf.MySQL.Builder;

public class UpdateQueryBuilder
{
    private string _tableName;

    public static UpdateQueryBuilder For<T>() => new UpdateQueryBuilder<T>();

    protected virtual string ResolveField(string field) => field;
    private readonly Dictionary<string, object> _fields = new();
    private readonly List<WhereCondition> _whereConditions = [];
// ... (skip unchanged lines) ...
    private int _paramCounter = 0;
    private bool _allowAll = false;
        
    private static readonly HashSet<string> AllowedOperators = new(StringComparer.OrdinalIgnoreCase)
    {
        "=", "<>", "!=", "<", "<=", ">", ">=", "LIKE", "NOT LIKE", "IN", "NOT IN", "IS NULL", "IS NOT NULL", "BETWEEN", "REGEXP", "NOT REGEXP"
    };
// ... (skip unchanged lines) ...
    public UpdateQueryBuilder Table(string tableName)
    {
        _tableName = tableName;
        return this;
    }

    /// <summary>
    /// Permite que a atualização seja executada em todas as linhas (sem WHERE).
    /// </summary>
    public UpdateQueryBuilder All()
    {
        _allowAll = true;
        return this;
    }

    public Task<int> ExecuteAsync(MySQL connection)
    {
        ArgumentNullException.ThrowIfNull(connection);
        if (_tableName == null) 
            throw new InvalidOperationException("Tabela não especificada");
        if (_fields.Count == 0) 
            throw new InvalidOperationException("Nenhum campo para atualizar");

        return connection.ExecuteUpdateAsync(this);
    }

    /// <summary>
    /// Executa o UPDATE e retorna a primeira linha afetada mapeada para o tipo T.
    /// Não suportado quando All() foi usado; nesse caso use ExecuteAsync(connection).
    /// </summary>
    /// <typeparam name="T">Tipo do modelo (deve ter construtor sem parâmetros e propriedades mapeáveis).</typeparam>
    /// <param name="connection">Conexão MySQL.</param>
    /// <returns>A entidade atualizada ou default se nenhuma linha foi afetada.</returns>
    public Task<T> ExecuteAsync<T>(MySQL connection) where T : new()
    {
        ArgumentNullException.ThrowIfNull(connection);
        if (_tableName == null) 
            throw new InvalidOperationException("Tabela não especificada");
        if (_fields.Count == 0) 
            throw new InvalidOperationException("Nenhum campo para atualizar");

        return connection.ExecuteUpdateAsync<T>(this);
    }

    private string EscapeIdentifier(string identifier)
    {
        if (string.IsNullOrEmpty(identifier)) return identifier;
        return identifier.Replace("`", "``");
    }

    private void ValidateOperator(string op)
    {
        if (!AllowedOperators.Contains(op))
        {
            throw new ArgumentException($"Operador não permitido: {op}");
        }
    }
        
    public UpdateQueryBuilder Set(string field, object value)
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

    public UpdateQueryBuilder Set(Dictionary<string, object> fields)
    {
        foreach (var field in fields)
        {
            _fields[ResolveField(field.Key)] = field.Value;
        }
        return this;
    }

    /// <summary>
    /// Define um campo com valor serializado como JSON.
    /// </summary>
    /// <param name="field">Nome do campo.</param>
    /// <param name="value">Objeto a ser serializado para JSON.</param>
    /// <returns>O builder para encadeamento.</returns>
    public UpdateQueryBuilder SetAsJson(string field, object value)
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
    /// Define um campo com valor serializado como JSON usando opções customizadas.
    /// </summary>
    /// <param name="field">Nome do campo.</param>
    /// <param name="value">Objeto a ser serializado para JSON.</param>
    /// <param name="options">Opções de serialização JSON.</param>
    /// <returns>O builder para encadeamento.</returns>
    public UpdateQueryBuilder SetAsJson(string field, object value, JsonSerializerOptions options)
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
    /// Define um campo GEOMETRY POINT usando ST_GeomFromWKB.
    /// </summary>
    /// <param name="field">Nome do campo.</param>
    /// <param name="point">Objeto Point a ser atualizado.</param>
    /// <returns>O builder para encadeamento.</returns>
    public UpdateQueryBuilder SetAsPoint(string field, Point point)
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
    /// Define um campo GEOMETRY POLYGON usando ST_PolygonFromText.
    /// </summary>
    /// <param name="field">Nome do campo.</param>
    /// <param name="polygon">Objeto Polygon a ser atualizado.</param>
    /// <returns>O builder para encadeamento.</returns>
    public UpdateQueryBuilder SetAsPolygon(string field, Polygon polygon)
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

    public UpdateQueryBuilder Where(string field, object value, string op = "=")
    {
        ValidateOperator(op);
        _whereConditions.Add(new WhereCondition 
        { 
            Field = ResolveField(field), 
            Value = value, 
            Operator = op,
            Logic = "AND"
        });
        return this;
    }

    public UpdateQueryBuilder Where(string field, object value, QueryOperator op)
    {
        return Where(field, value, op.ToSqlString());
    }

    public UpdateQueryBuilder WhereIn<T>(string field, IEnumerable<T> values)
    {
        _whereConditions.Add(new WhereCondition 
        { 
            Field = ResolveField(field), 
            Values = MaterializeValues(values),
            Operator = "IN",
            Logic = "AND"
        });
        return this;
    }

    public UpdateQueryBuilder WhereNotIn<T>(string field, IEnumerable<T> values)
    {
        _whereConditions.Add(new WhereCondition 
        { 
            Field = ResolveField(field), 
            Values = MaterializeValues(values),
            Operator = "NOT IN",
            Logic = "AND"
        });
        return this;
    }

    public UpdateQueryBuilder OrWhere(string field, object value, string op = "=")
    {
        ValidateOperator(op);
        _whereConditions.Add(new WhereCondition 
        { 
            Field = ResolveField(field), 
            Value = value, 
            Operator = op,
            Logic = "OR"
        });
        return this;
    }

    public UpdateQueryBuilder WhereNull(string field)
    {
        _whereConditions.Add(new WhereCondition 
        { 
            Field = ResolveField(field), 
            Operator = "IS NULL",
            Logic = "AND"
        });
        return this;
    }

    public UpdateQueryBuilder WhereNotNull(string field)
    {
        _whereConditions.Add(new WhereCondition 
        { 
            Field = ResolveField(field), 
            Operator = "IS NOT NULL",
            Logic = "AND"
        });
        return this;
    }

    public UpdateQueryBuilder WhereBetween(string field, object start, object end)
    {
        _whereConditions.Add(new WhereCondition 
        { 
            Field = ResolveField(field),
            Value = start,
            SecondValue = end,
            Operator = "BETWEEN",
            Logic = "AND"
        });
        return this;
    }

    public UpdateQueryBuilder WhereLike(string field, string pattern)
    {
        _whereConditions.Add(new WhereCondition 
        { 
            Field = ResolveField(field),
            Value = pattern,
            Operator = "LIKE",
            Logic = "AND"
        });
        return this;
    }

    public (string Sql, MySqlCommand Command) Build()
    {
        _paramCounter = 0;
        if (string.IsNullOrEmpty(_tableName))
            throw new InvalidOperationException("Nome da tabela não definido");
            
        if (_fields.Count == 0)
            throw new InvalidOperationException("Nenhum campo para atualizar");
            
        if (_whereConditions.Count == 0 && !_allowAll)
            throw new InvalidOperationException("Nenhuma condição WHERE definida. Use .All() se realmente deseja atualizar todas as linhas.");

        var command = new MySqlCommand();
            
        // SET clause
        var setClauses = new List<string>(_fields.Count);
        foreach (var field in _fields)
        {
            var paramName = GetNextParamName();
            var escapedField = EscapeIdentifier(field.Key);
                
            if (field.Value is PointValue pointValue)
            {
                setClauses.Add($"`{escapedField}` = ST_GeomFromWKB(@{paramName}, {pointValue.Point.SRID})");
                AddParameter(command, paramName, pointValue.Point.ToWKB());
            }
            else if (field.Value is PolygonValue polygonValue)
            {
                setClauses.Add($"`{escapedField}` = ST_PolygonFromText(@{paramName}, {polygonValue.Polygon.SRID})");
                AddParameter(command, paramName, polygonValue.Polygon.ToWKT());
            }
            else
            {
                setClauses.Add($"`{escapedField}` = @{paramName}");
                AddParameter(command, paramName, field.Value);
            }
        }

        // WHERE clause
        var whereClauses = new List<string>(_whereConditions.Count);
        for (int i = 0; i < _whereConditions.Count; i++)
        {
            var condition = _whereConditions[i];
            var logic = i > 0 ? condition.Logic : "";
                
            string clause = BuildWhereClause(condition, command);

            if (i > 0)
                whereClauses.Add($"{logic} {clause}");
            else
                whereClauses.Add(clause);
        }

        var sql = $"UPDATE `{EscapeIdentifier(_tableName)}` SET {string.Join(", ", setClauses)}";

        if (whereClauses.Count > 0)
        {
            sql += $" WHERE {string.Join(" ", whereClauses)}";
        }
        command.CommandText = sql;
            
        return (sql, command);
    }

    /// <summary>
    /// Constrói um SELECT * para as mesmas condições WHERE do UPDATE.
    /// Usado por ExecuteAsync&lt;T&gt; para retornar a(s) linha(s) atualizada(s).
    /// </summary>
    internal (string Sql, MySqlCommand Command) BuildSelect()
    {
        if (_whereConditions.Count == 0 && !_allowAll)
            throw new InvalidOperationException("Nenhuma condição WHERE definida para SELECT.");
        if (_allowAll)
            throw new InvalidOperationException("ExecuteAsync<T> não é suportado quando All() foi usado. Use ExecuteAsync() para obter o número de linhas afetadas.");

        var command = new MySqlCommand();
        var savedCounter = _paramCounter;
        _paramCounter = 0;

        var whereClauses = new List<string>(_whereConditions.Count);
        for (int i = 0; i < _whereConditions.Count; i++)
        {
            var condition = _whereConditions[i];
            var logic = i > 0 ? condition.Logic : "";
            string clause = BuildWhereClause(condition, command);
            if (i > 0)
                whereClauses.Add($"{logic} {clause}");
            else
                whereClauses.Add(clause);
        }

        var sql = $"SELECT * FROM `{EscapeIdentifier(_tableName)}` WHERE {string.Join(" ", whereClauses)}";
        command.CommandText = sql;
        _paramCounter = savedCounter;
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

    private string BuildWhereClause(WhereCondition condition, MySqlCommand command)
    {
        var escapedField = EscapeIdentifier(condition.Field);

        switch (condition.Operator)
        {
            case "IS NULL":
            case "IS NOT NULL":
                return $"`{escapedField}` {condition.Operator}";

            case "IN":
            case "NOT IN":
                var inParams = new List<string>(condition.Values.Count);
                foreach (var val in condition.Values)
                {
                    var nextParamName = GetNextParamName();
                    inParams.Add($"@{nextParamName}");
                    AddParameter(command, nextParamName, val);
                }
                return $"`{escapedField}` {condition.Operator} ({string.Join(", ", inParams)})";

            case "BETWEEN":
                var startParam = GetNextParamName();
                var endParam = GetNextParamName();
                AddParameter(command, startParam, condition.Value);
                AddParameter(command, endParam, condition.SecondValue);
                return $"`{escapedField}` BETWEEN @{startParam} AND @{endParam}";

            default:
                var paramName = GetNextParamName();
                AddParameter(command, paramName, condition.Value);
                return $"`{escapedField}` {condition.Operator} @{paramName}";
        }
    }

    private string GetNextParamName()
    {
        return $"p{_paramCounter++}";
    }

    private static List<object> MaterializeValues<T>(IEnumerable<T> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        if (values is ICollection<T> collection)
        {
            var result = new List<object>(collection.Count);
            foreach (var value in collection)
            {
                result.Add(value);
            }

            return result;
        }

        var list = new List<object>();
        foreach (var value in values)
        {
            list.Add(value);
        }

        return list;
    }

    private static void AddParameter(MySqlCommand command, string paramName, object value)
    {
        var parameterName = $"@{paramName}";
        var parameter = new MySqlParameter(parameterName, value ?? DBNull.Value);

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

    private class WhereCondition
    {
        public string Field { get; set; }
        public object Value { get; set; }
        public object SecondValue { get; set; }
        public List<object> Values { get; set; }
        public string Operator { get; set; }
        public string Logic { get; set; }
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

public class UpdateQueryBuilder<T> : UpdateQueryBuilder
{
    private static readonly Dictionary<string, string> FieldMapping = CreateFieldMapping();
    private static readonly string ResolvedTableName = GetTableName<T>();

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

    public UpdateQueryBuilder()
    {
        Table(ResolvedTableName);
    }

    protected override string ResolveField(string field)
    {
        return FieldMapping.TryGetValue(field, out var column) ? column : field;
    }
}

public class UpdateQueryExecutor
{
    private readonly MySqlConnection _connection;
    private MySqlTransaction _transaction;

    public UpdateQueryExecutor(MySqlConnection connection)
    {
        _connection = connection;
    }

    public UpdateQueryExecutor(MySqlConnection connection, MySqlTransaction transaction)
    {
        _connection = connection;
        _transaction = transaction;
    }

    // Executar com builder
    public async Task<int> ExecuteAsync(UpdateQueryBuilder builder)
    {
        var (_, command) = builder.Build();
        await using (command)
        {
            return await ExecuteCommandAsync(command);
        }
    }

    // Executar síncrono
    public int Execute(UpdateQueryBuilder builder)
    {
        var (_, command) = builder.Build();
        using (command)
        {
            return ExecuteCommand(command);
        }
    }

    // Executar direto com dicionários
    public async Task<int> ExecuteAsync(
        string tableName, 
        Dictionary<string, object> fields, 
        Dictionary<string, object> where)
    {
        var builder = new UpdateQueryBuilder()
            .Table(tableName)
            .Set(fields);

        foreach (var condition in where)
        {
            builder.Where(condition.Key, condition.Value);
        }

        return await ExecuteAsync(builder);
    }

    // Executar e retornar linhas afetadas com validação
    public async Task<UpdateResult> ExecuteWithResultAsync(UpdateQueryBuilder builder)
    {
        var stopwatch = Stopwatch.StartNew();
        var (sql, command) = builder.Build();

        try
        {
            await using (command)
            {
                var rowsAffected = await ExecuteCommandAsync(command);

                return new UpdateResult
                {
                    Success = true,
                    RowsAffected = rowsAffected,
                    Sql = sql,
                    ExecutionTime = stopwatch.Elapsed
                };
            }
        }
        catch (Exception ex)
        {
            return new UpdateResult
            {
                Success = false,
                RowsAffected = 0,
                Sql = sql,
                ExecutionTime = stopwatch.Elapsed,
                Error = ex.Message,
                Exception = ex
            };
        }
    }

    private async Task<int> ExecuteCommandAsync(MySqlCommand command)
    {
        command.Connection = _connection;

        if (_transaction != null)
            command.Transaction = _transaction;

        return await command.ExecuteNonQueryAsync();
    }

    private int ExecuteCommand(MySqlCommand command)
    {
        command.Connection = _connection;

        if (_transaction != null)
            command.Transaction = _transaction;

        return command.ExecuteNonQuery();
    }
}

public class UpdateResult
{
    public bool Success { get; set; }
    public int RowsAffected { get; set; }
    public string Sql { get; set; }
    public TimeSpan ExecutionTime { get; set; }
    public string Error { get; set; }
    public Exception Exception { get; set; }
}