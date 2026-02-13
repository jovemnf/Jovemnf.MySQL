using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using MySqlConnector;

namespace Jovemnf.MySQL.Builder;

public class SelectQueryBuilder
{
    protected string _tableName;

    public static SelectQueryBuilder For<T>() => new SelectQueryBuilder<T>();
    private List<string> _fields = new List<string>();
    private List<JoinClause> _joins = new List<JoinClause>();
    private List<WhereCondition> _whereConditions = new List<WhereCondition>();
    private List<string> _orderBys = new List<string>();
    private int? _limit;
    private int? _offset;
    private int _paramCounter = 0;

    private static readonly HashSet<string> _allowedOperators = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "=", "<>", "!=", "<", "<=", ">", ">=", "LIKE", "NOT LIKE", "IN", "NOT IN", "IS NULL", "IS NOT NULL", "BETWEEN", "REGEXP", "NOT REGEXP"
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

    public SelectQueryBuilder Select(params string[] fields)
    {
        _fields.AddRange(fields.Select(ResolveField));
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
        _joins.Add(new JoinClause 
        { 
            Table = table, 
            First = first.Contains(".") ? first : ResolveField(first), 
            Operator = op, 
            Second = second.Contains(".") ? second : ResolveField(second), 
            Type = type 
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

    public SelectQueryBuilder OrWhere(string field, object value, string op = "=")
    {
        ValidateOperator(op);
        _whereConditions.Add(new WhereCondition { Field = ResolveField(field), Value = value, Operator = op, Logic = "OR" });
        return this;
    }

    public SelectQueryBuilder WhereIn<T>(string field, IEnumerable<T> values)
    {
        _whereConditions.Add(new WhereCondition { Field = ResolveField(field), Values = values.Cast<object>().ToList(), Operator = "IN", Logic = "AND" });
        return this;
    }

    public SelectQueryBuilder WhereNotIn<T>(string field, IEnumerable<T> values)
    {
        _whereConditions.Add(new WhereCondition { Field = ResolveField(field), Values = values.Cast<object>().ToList(), Operator = "NOT IN", Logic = "AND" });
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
        _orderBys.Add($"{EscapeIdentifier(ResolveField(field))} {direction.ToUpper()}");
        return this;
    }

    public SelectQueryBuilder Limit(int limit, int offset = 0)
    {
        _limit = limit;
        _offset = offset;
        return this;
    }

    public (string Sql, MySqlCommand Command) Build()
    {
        _paramCounter = 0;
        if (string.IsNullOrEmpty(_tableName))
            throw new InvalidOperationException("Nome da tabela não definido");

        var command = new MySqlCommand();
        var selection = _fields.Count > 0 ? string.Join(", ", _fields.Select(f => EscapeIdentifier(f))) : "*";
        
        var sql = $"SELECT {selection} FROM {EscapeIdentifier(_tableName)}";

        // JOINS
        foreach (var join in _joins)
        {
            sql += $" {join.Type} JOIN {EscapeIdentifier(join.Table)} ON {EscapeIdentifier(join.First)} {join.Operator} {EscapeIdentifier(join.Second)}";
        }

        // WHERE
        if (_whereConditions.Count > 0)
        {
            var whereClauses = new List<string>();
            for (int i = 0; i < _whereConditions.Count; i++)
            {
                var condition = _whereConditions[i];
                var logic = i > 0 ? condition.Logic : "";
                string clause = BuildWhereClause(condition, command);
                if (i > 0) whereClauses.Add($"{logic} {clause}");
                else whereClauses.Add(clause);
            }
            sql += $" WHERE {string.Join(" ", whereClauses)}";
        }

        // ORDER BY
        if (_orderBys.Count > 0)
        {
            sql += $" ORDER BY {string.Join(", ", _orderBys)}";
        }

        // LIMIT
        if (_limit.HasValue)
        {
            sql += $" LIMIT {_limit.Value}";
            if (_offset.HasValue && _offset.Value > 0)
            {
                sql += $" OFFSET {_offset.Value}";
            }
        }

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

    private string BuildWhereClause(WhereCondition condition, MySqlCommand command)
    {
        switch (condition.Operator)
        {
            case "IS NULL":
            case "IS NOT NULL":
                return $"{EscapeIdentifier(condition.Field)} {condition.Operator}";

            case "IN":
            case "NOT IN":
                var inParams = new List<string>();
                foreach (var val in condition.Values)
                {
                    var name = GetNextParamName();
                    inParams.Add($"@{name}");
                    command.Parameters.AddWithValue($"@{name}", val ?? DBNull.Value);
                }
                return $"{EscapeIdentifier(condition.Field)} {condition.Operator} ({string.Join(", ", inParams)})";

            case "BETWEEN":
                var start = GetNextParamName();
                var end = GetNextParamName();
                command.Parameters.AddWithValue($"@{start}", condition.Value ?? DBNull.Value);
                command.Parameters.AddWithValue($"@{end}", condition.SecondValue ?? DBNull.Value);
                return $"{EscapeIdentifier(condition.Field)} BETWEEN @{start} AND @{end}";

            default:
                var pName = GetNextParamName();
                command.Parameters.AddWithValue($"@{pName}", condition.Value ?? DBNull.Value);
                return $"{EscapeIdentifier(condition.Field)} {condition.Operator} @{pName}";
        }
    }

    private string GetNextParamName() => $"p{_paramCounter++}";

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
    }

    protected virtual string ResolveField(string field) => field;
}

public class SelectQueryBuilder<T> : SelectQueryBuilder
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

    public SelectQueryBuilder()
    {
        Table(GetTableName<T>());
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
}
