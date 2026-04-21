using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Text;
using System.Threading.Tasks;
using MySqlConnector;

namespace Jovemnf.MySQL.Builder;

public class SelectQueryBuilder
{
    private string _tableName;

    public static SelectQueryBuilder For<T>() => new SelectQueryBuilder<T>();
    private List<SelectionField> _fields = new List<SelectionField>();
    private List<JoinClause> _joins = new List<JoinClause>();
    private List<WhereCondition> _whereConditions = new List<WhereCondition>();
    private List<string> _orderBys = new List<string>();
    private int? _limit;
    private int? _offset;
    private int _paramCounter = 0;
    private static readonly HashSet<string> _allowedJoinTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "INNER", "LEFT", "RIGHT"
    };
    private static readonly HashSet<string> _allowedOrderDirections = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "ASC", "DESC"
    };

    private static readonly HashSet<string> _allowedOperators = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "=", "<>", "!=", "<", "<=", ">", ">=", "LIKE", "NOT LIKE", "IN", "NOT IN", "IS NULL", "IS NOT NULL", "BETWEEN", "REGEXP", "NOT REGEXP", "RAW"
    };

    private string EscapeIdentifier(string identifier)
    {
        if (string.IsNullOrEmpty(identifier)) return identifier;
        if (identifier == "*") return "*";
        
        // Handle table.column
        if (identifier.Contains("."))
        {
            var parts = identifier.Split('.');
            return string.Join(".", parts.Select(p => p == "*" ? "*" : $"`{p.Replace("`", "``")}`"));
        }
        
        return $"`{identifier.Replace("`", "``")}`";
    }

    private void ValidateOperator(string op)
    {
        if (!_allowedOperators.Contains(op))
        {
            throw new ArgumentException($"Operador não permitido: {op}");
        }
    }

    private void ValidateJoinType(string type)
    {
        if (!_allowedJoinTypes.Contains(type))
        {
            throw new ArgumentException($"Tipo de JOIN não permitido: {type}");
        }
    }

    private void ValidateOrderDirection(string direction)
    {
        if (!_allowedOrderDirections.Contains(direction))
        {
            throw new ArgumentException($"Direção de ordenação não permitida: {direction}");
        }
    }
    
    public Task ExecuteAsync(MySQL connection)
    {
        ArgumentNullException.ThrowIfNull(connection);
        if (_tableName == null) 
            throw new InvalidOperationException("Tabela não especificada");
        return connection.ExecuteQueryAsync(this);
    }

    /// <summary>
    /// Executa o SELECT e retorna todas as linhas mapeadas para o tipo T.
    /// </summary>
    /// <typeparam name="T">Tipo do modelo (deve ter construtor sem parâmetros e propriedades mapeáveis).</typeparam>
    /// <param name="connection">Conexão MySQL.</param>
    /// <returns>Lista das entidades do resultado da consulta.</returns>
    public Task<List<T>> ExecuteAsync<T>(MySQL connection) where T : new()
    {
        ArgumentNullException.ThrowIfNull(connection);
        if (_tableName == null) 
            throw new InvalidOperationException("Tabela não especificada");
        return connection.ExecuteQueryAsync<T>(this);
    }

    /// <summary>
    /// Verifica se existe pelo menos um registro que atenda aos filtros informados.
    /// </summary>
    public bool ExistsSync(MySQL connection)
    {
        ArgumentNullException.ThrowIfNull(connection);
        if (_tableName == null)
            throw new InvalidOperationException("Tabela não especificada");
        return connection.ExecuteExistsSync(this);
    }

    /// <summary>
    /// Verifica de forma assíncrona se existe pelo menos um registro que atenda aos filtros informados.
    /// </summary>
    public Task<bool> ExistsAsync(MySQL connection)
    {
        ArgumentNullException.ThrowIfNull(connection);
        if (_tableName == null)
            throw new InvalidOperationException("Tabela não especificada");
        return connection.ExecuteExistsAsync(this);
    }

    public SelectQueryBuilder Select(params string[] fields)
    {
        foreach (var field in fields)
        {
            _fields.Add(new SelectionField { Name = ResolveField(field), IsRaw = false });
        }

        return this;
    }

    public SelectQueryBuilder Select<TSelection>(TSelection selection)
    {
        ArgumentNullException.ThrowIfNull(selection);
        return Select(selection.GetType());
    }

    public SelectQueryBuilder Select<TSelection>()
    {
        return Select(typeof(TSelection));
    }

    public SelectQueryBuilder Select(Type selectionType)
    {
        ArgumentNullException.ThrowIfNull(selectionType);
        return AddSelectionFields(selectionType);
    }

    public SelectQueryBuilder SelectRaw(string sql)
    {
        _fields.Add(new SelectionField { Name = sql, IsRaw = true });
        return this;
    }

    public SelectQueryBuilder Count(string field = "*")
    {
        _fields.Clear();
        string countField = field == "*" ? "*" : EscapeIdentifier(ResolveField(field));
        _fields.Add(new SelectionField { Name = $"COUNT({countField})", IsRaw = true });
        return this;
    }

    public SelectQueryBuilder Table(string tableName)
    {
        _tableName = tableName;
        return this;
    }

    public SelectQueryBuilder From(string tableName) => Table(tableName);

    public SelectQueryBuilder Join(string table, string first, string op, string second, string type = "INNER")
    {
        ValidateOperator(op);
        ValidateJoinType(type);

        _joins.Add(new JoinClause 
        { 
            Table = table, 
            First = first.Contains(".") ? first : ResolveField(first), 
            Operator = op, 
            Second = second.Contains(".") ? second : ResolveField(second), 
            Type = type.ToUpperInvariant()
        });
        return this;
    }

    public SelectQueryBuilder LeftJoin(string table, string first, string op, string second) => Join(table, first, op, second, "LEFT");
    public SelectQueryBuilder RightJoin(string table, string first, string op, string second) => Join(table, first, op, second, "RIGHT");

    public SelectQueryBuilder Where(string field, object value, string op = "=")
    {
        ValidateOperator(op);
        _whereConditions.Add(new WhereCondition { Field = ResolveField(field), Value = value, Operator = op, Logic = "AND" });
        return this;
    }

    public SelectQueryBuilder WhereRaw(string sql, params object[] parameters)
    {
        _whereConditions.Add(new WhereCondition { RawSql = sql, RawParameters = parameters, Operator = "RAW", Logic = "AND" });
        return this;
    }

    public SelectQueryBuilder OrWhere(string field, object value, string op = "=")
    {
        ValidateOperator(op);
        _whereConditions.Add(new WhereCondition { Field = ResolveField(field), Value = value, Operator = op, Logic = "OR" });
        return this;
    }

    public SelectQueryBuilder OrWhereRaw(string sql, params object[] parameters)
    {
        _whereConditions.Add(new WhereCondition { RawSql = sql, RawParameters = parameters, Operator = "RAW", Logic = "OR" });
        return this;
    }

    public SelectQueryBuilder WhereIn<T>(string field, IEnumerable<T> values)
    {
        _whereConditions.Add(new WhereCondition { Field = ResolveField(field), Values = MaterializeValues(values), Operator = "IN", Logic = "AND" });
        return this;
    }

    public SelectQueryBuilder WhereNotIn<T>(string field, IEnumerable<T> values)
    {
        _whereConditions.Add(new WhereCondition { Field = ResolveField(field), Values = MaterializeValues(values), Operator = "NOT IN", Logic = "AND" });
        return this;
    }

    public SelectQueryBuilder WhereNull(string field)
    {
        _whereConditions.Add(new WhereCondition { Field = ResolveField(field), Operator = "IS NULL", Logic = "AND" });
        return this;
    }

    public SelectQueryBuilder WhereNotNull(string field)
    {
        _whereConditions.Add(new WhereCondition { Field = ResolveField(field), Operator = "IS NOT NULL", Logic = "AND" });
        return this;
    }

    public SelectQueryBuilder WhereBetween(string field, object start, object end)
    {
        _whereConditions.Add(new WhereCondition { Field = ResolveField(field), Value = start, SecondValue = end, Operator = "BETWEEN", Logic = "AND" });
        return this;
    }

    public SelectQueryBuilder WhereLike(string field, string pattern)
    {
        _whereConditions.Add(new WhereCondition { Field = ResolveField(field), Value = pattern, Operator = "LIKE", Logic = "AND" });
        return this;
    }

    public SelectQueryBuilder OrderBy(string field, string direction = "ASC")
    {
        ValidateOrderDirection(direction);
        _orderBys.Add($"{EscapeIdentifier(ResolveField(field))} {direction.ToUpperInvariant()}");
        return this;
    }

    public SelectQueryBuilder Limit(int limit, int offset = 0)
    {
        if (limit < 0)
            throw new ArgumentOutOfRangeException(nameof(limit), "O limite não pode ser negativo.");
        if (offset < 0)
            throw new ArgumentOutOfRangeException(nameof(offset), "O offset não pode ser negativo.");

        _limit = limit;
        _offset = offset;
        return this;
    }

    public (string Sql, MySqlCommand Command) Build()
    {
        return BuildQuery(BuildSelectionClause(), includeOrderBy: true, limit: _limit, offset: _offset);
    }

    /// <summary>
    /// Monta uma consulta EXISTS baseada na tabela, JOINs e WHEREs atuais.
    /// </summary>
    public (string Sql, MySqlCommand Command) BuildExists()
    {
        _paramCounter = 0;
        if (string.IsNullOrEmpty(_tableName))
            throw new InvalidOperationException("Nome da tabela não definido");

        var command = new MySqlCommand();
        var sqlBuilder = new StringBuilder();
        sqlBuilder.Append("SELECT EXISTS(SELECT 1 FROM ");
        sqlBuilder.Append(EscapeIdentifier(_tableName));
        AppendJoinsAndWhere(sqlBuilder, command);
        sqlBuilder.Append(" LIMIT 1)");

        var sql = sqlBuilder.ToString();
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

    public string ToDebugSql()
    {
        var (sql, command) = Build();
        return ReplaceParameters(sql, command.Parameters);
    }

    private string BuildWhereClause(WhereCondition condition, MySqlCommand command)
    {
        if (condition.Operator == "RAW")
        {
            string sql = condition.RawSql;
            if (condition.RawParameters != null)
            {
                var parameterTokens = new List<(string Token, string ParameterName)>(condition.RawParameters.Length);
                for (int i = 0; i < condition.RawParameters.Length; i++)
                {
                    var pName = GetNextParamName();
                    var parameterName = $"@{pName}";
                    var token = $"__raw_param_{i}__";
                    sql = sql.Replace("{" + i + "}", token);
                    sql = Regex.Replace(sql, $@"{Regex.Escape($"@p{i}")}(?!\w)", token);
                    parameterTokens.Add((token, parameterName));
                    AddParameter(command, pName, condition.RawParameters[i]);
                }

                foreach (var (token, parameterName) in parameterTokens)
                {
                    sql = sql.Replace(token, parameterName);
                }
            }
            return sql;
        }

        var escapedField = EscapeIdentifier(condition.Field);

        switch (condition.Operator)
        {
            case "IS NULL":
            case "IS NOT NULL":
                return $"{escapedField} {condition.Operator}";

            case "IN":
            case "NOT IN":
                var inParams = new List<string>(condition.Values.Count);
                foreach (var val in condition.Values)
                {
                    var name = GetNextParamName();
                    inParams.Add($"@{name}");
                    AddParameter(command, name, val);
                }
                return $"{escapedField} {condition.Operator} ({string.Join(", ", inParams)})";

            case "BETWEEN":
                var start = GetNextParamName();
                var end = GetNextParamName();
                AddParameter(command, start, condition.Value);
                AddParameter(command, end, condition.SecondValue);
                return $"{escapedField} BETWEEN @{start} AND @{end}";

            default:
                var pName = GetNextParamName();
                AddParameter(command, pName, condition.Value);
                return $"{escapedField} {condition.Operator} @{pName}";
        }
    }

    private string GetNextParamName() => $"p{_paramCounter++}";

    private static string ReplaceParameters(string sql, MySqlParameterCollection parameters)
    {
        var debugSql = sql;

        foreach (var parameter in parameters
                     .OrderByDescending(p => p.ParameterName.Length))
        {
            debugSql = Regex.Replace(
                debugSql,
                $@"{Regex.Escape(parameter.ParameterName)}(?!\w)",
                FormatParameterValue(parameter.Value));
        }

        return debugSql;
    }

    private static string FormatParameterValue(object value)
    {
        if (value == null || value == DBNull.Value)
            return "NULL";

        return value switch
        {
            string s => $"'{s.Replace("'", "''")}'",
            char c => $"'{c.ToString().Replace("'", "''")}'",
            bool b => b ? "1" : "0",
            DateTime dt => $"'{dt:yyyy-MM-dd HH:mm:ss.fffffff}'",
            DateTimeOffset dto => $"'{dto:yyyy-MM-dd HH:mm:ss.fffffff zzz}'",
            Guid guid => $"'{guid}'",
            byte[] bytes => $"0x{Convert.ToHexString(bytes)}",
            Enum e => Convert.ToInt64(e, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture),
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => $"'{value.ToString()?.Replace("'", "''")}'"
        };
    }

    private string BuildSelectionClause()
    {
        if (_fields.Count == 0)
            return "*";

        var selections = new List<string>(_fields.Count);
        foreach (var field in _fields)
        {
            selections.Add(field.IsRaw ? field.Name : EscapeIdentifier(field.Name));
        }

        return string.Join(", ", selections);
    }

    private (string Sql, MySqlCommand Command) BuildQuery(string selection, bool includeOrderBy, int? limit, int? offset)
    {
        _paramCounter = 0;
        if (string.IsNullOrEmpty(_tableName))
            throw new InvalidOperationException("Nome da tabela não definido");

        var command = new MySqlCommand();
        var sqlBuilder = new StringBuilder();
        sqlBuilder.Append("SELECT ");
        sqlBuilder.Append(selection);
        sqlBuilder.Append(" FROM ");
        sqlBuilder.Append(EscapeIdentifier(_tableName));
        AppendJoinsAndWhere(sqlBuilder, command);

        if (includeOrderBy && _orderBys.Count > 0)
        {
            sqlBuilder.Append(" ORDER BY ");
            sqlBuilder.Append(string.Join(", ", _orderBys));
        }

        if (limit.HasValue)
        {
            sqlBuilder.Append(" LIMIT ");
            sqlBuilder.Append(limit.Value);
            if (offset.HasValue && offset.Value > 0)
            {
                sqlBuilder.Append(" OFFSET ");
                sqlBuilder.Append(offset.Value);
            }
        }

        var sql = sqlBuilder.ToString();
        command.CommandText = sql;
        return (sql, command);
    }

    private void AppendJoinsAndWhere(StringBuilder sqlBuilder, MySqlCommand command)
    {
        foreach (var join in _joins)
        {
            sqlBuilder.Append(' ');
            sqlBuilder.Append(join.Type);
            sqlBuilder.Append(" JOIN ");
            sqlBuilder.Append(EscapeIdentifier(join.Table));
            sqlBuilder.Append(" ON ");
            sqlBuilder.Append(EscapeIdentifier(join.First));
            sqlBuilder.Append(' ');
            sqlBuilder.Append(join.Operator);
            sqlBuilder.Append(' ');
            sqlBuilder.Append(EscapeIdentifier(join.Second));
        }

        if (_whereConditions.Count > 0)
        {
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

            sqlBuilder.Append(" WHERE ");
            sqlBuilder.Append(string.Join(" ", whereClauses));
        }
    }

    private SelectQueryBuilder AddSelectionFields(Type selectionType)
    {
        foreach (var field in ResolveSelectionFields(selectionType))
        {
            _fields.Add(new SelectionField { Name = field, IsRaw = false });
        }

        return this;
    }

    private IEnumerable<string> ResolveSelectionFields(Type selectionType)
    {
        ArgumentNullException.ThrowIfNull(selectionType);

        var properties = selectionType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        if (properties.Length == 0)
        {
            throw new ArgumentException($"O tipo {selectionType.Name} não possui propriedades públicas para seleção.", nameof(selectionType));
        }

        foreach (var property in properties)
        {
            var fieldName = ResolveSelectionFieldName(property);
            yield return fieldName;
        }
    }

    private string ResolveSelectionFieldName(PropertyInfo property)
    {
        var attr = property.GetCustomAttribute<DbFieldAttribute>(true);
        if (attr?.Name != null)
            return attr.Name;

        var jsonAttr = property.GetCustomAttribute<JsonPropertyNameAttribute>(true);
        if (!string.IsNullOrWhiteSpace(jsonAttr?.Name))
            return jsonAttr.Name;

        var resolvedField = ResolveField(property.Name);
        return string.Equals(resolvedField, property.Name, StringComparison.Ordinal)
            ? property.Name.ToSnakeCase()
            : resolvedField;
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

    private class SelectionField
    {
        public string Name { get; set; }
        public bool IsRaw { get; set; }
    }

    private class JoinClause
    {
        public string Table { get; set; }
        public string First { get; set; }
        public string Operator { get; set; }
        public string Second { get; set; }
        public string Type { get; set; }
    }

    private class WhereCondition
    {
        public string Field { get; set; }
        public object Value { get; set; }
        public object SecondValue { get; set; }
        public List<object> Values { get; set; }
        public string Operator { get; set; }
        public string Logic { get; set; }
        public string RawSql { get; set; }
        public object[] RawParameters { get; set; }
    }

    protected virtual string ResolveField(string field) => field;

}

public class SelectQueryBuilder<T> : SelectQueryBuilder
{
    private static readonly Dictionary<string, string> _fieldMapping = CreateFieldMapping();
    private static readonly string _resolvedTableName = GetTableName<T>();

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

    public SelectQueryBuilder()
    {
        Table(_resolvedTableName);
    }

    protected override string ResolveField(string field)
    {
        return _fieldMapping.TryGetValue(field, out var column) ? column : field;
    }
}

public class SelectQueryExecutor
{
    private readonly MySqlConnection _connection;
    private MySqlTransaction _transaction;

    public SelectQueryExecutor(MySqlConnection connection) => _connection = connection;
    public SelectQueryExecutor(MySqlConnection connection, MySqlTransaction transaction)
    {
        _connection = connection;
        _transaction = transaction;
    }

    public async Task<MySQLReader> ExecuteQueryAsync(SelectQueryBuilder builder)
    {
        var (_, command) = builder.Build();
        command.Connection = _connection;
        if (_transaction != null) command.Transaction = _transaction;
        return new MySQLReader(await command.ExecuteReaderAsync());
    }

    public MySQLReader ExecuteQuerySync(SelectQueryBuilder builder)
    {
        var (_, command) = builder.Build();
        command.Connection = _connection;
        if (_transaction != null) command.Transaction = _transaction;
        return new MySQLReader(command.ExecuteReader());
    }

    public async Task<long> ExecuteCountAsync(SelectQueryBuilder builder)
    {
        var (_, command) = builder.Build();
        command.Connection = _connection;
        if (_transaction != null) command.Transaction = _transaction;
        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt64(result);
    }

    public long ExecuteCountSync(SelectQueryBuilder builder)
    {
        var (_, command) = builder.Build();
        command.Connection = _connection;
        if (_transaction != null) command.Transaction = _transaction;
        var result = command.ExecuteScalar();
        return Convert.ToInt64(result);
    }

    public async Task<bool> ExecuteExistsAsync(SelectQueryBuilder builder)
    {
        var (_, command) = builder.BuildExists();
        command.Connection = _connection;
        if (_transaction != null) command.Transaction = _transaction;
        var result = await command.ExecuteScalarAsync();
        return result != null && result != DBNull.Value && Convert.ToInt64(result) > 0;
    }

    public bool ExecuteExistsSync(SelectQueryBuilder builder)
    {
        var (_, command) = builder.BuildExists();
        command.Connection = _connection;
        if (_transaction != null) command.Transaction = _transaction;
        var result = command.ExecuteScalar();
        return result != null && result != DBNull.Value && Convert.ToInt64(result) > 0;
    }
}
