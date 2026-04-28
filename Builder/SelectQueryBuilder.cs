using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MySqlConnector;

namespace Jovemnf.MySQL.Builder;

public class SelectQueryBuilder
{
    private string? _tableName;

    public static SelectQueryBuilder<T> For<T>() => new SelectQueryBuilder<T>();
    private List<SelectionField> _fields = new List<SelectionField>();
    private List<JoinClause> _joins = new List<JoinClause>();
    private List<WhereCondition> _whereConditions = new List<WhereCondition>();
    private List<WhereCondition> _havingConditions = new List<WhereCondition>();
    private List<string> _groupBys = new List<string>();
    private List<string> _orderBys = new List<string>();
    private int? _limit;
    private int? _offset;
    private int _paramCounter = 0;
    private bool _distinct;
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

    private string EscapeIdentifier(string? identifier)
    {
        if (string.IsNullOrEmpty(identifier)) return string.Empty;
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

    public Task<MySqlReader> ExecuteAsync(MySQL connection, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(connection);
        if (_tableName == null)
            throw new InvalidOperationException("Tabela não especificada");
        return connection.ExecuteQueryAsync(this, cancellationToken);
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

    public Task<List<T>> ExecuteAsync<T>(MySQL connection, CancellationToken cancellationToken) where T : new()
    {
        ArgumentNullException.ThrowIfNull(connection);
        if (_tableName == null)
            throw new InvalidOperationException("Tabela não especificada");
        return connection.ExecuteQueryAsync<T>(this, cancellationToken);
    }

    /// <summary>
    /// Executa o SELECT em modo streaming (<see cref="IAsyncEnumerable{T}"/>), mapeando cada
    /// linha para <typeparamref name="T"/> sob demanda. Ideal para grandes volumes de dados —
    /// nada é materializado em lista.
    /// </summary>
    /// <typeparam name="T">Tipo do modelo (deve ter construtor sem parâmetros e propriedades mapeáveis).</typeparam>
    /// <param name="connection">Conexão MySQL aberta.</param>
    /// <param name="cancellationToken">Token de cancelamento cooperativo.</param>
    public IAsyncEnumerable<T> StreamAsync<T>(MySQL connection, CancellationToken cancellationToken = default) where T : new()
    {
        ArgumentNullException.ThrowIfNull(connection);
        if (_tableName == null)
            throw new InvalidOperationException("Tabela não especificada");
        return connection.ExecuteQueryStreamAsync<T>(this, cancellationToken);
    }

    /// <summary>
    /// Versão de <see cref="StreamAsync{T}(MySQL, CancellationToken)"/> que abre/fecha a conexão
    /// automaticamente via <see cref="DatabaseHelper"/>.
    /// </summary>
    public IAsyncEnumerable<T> StreamAsync<T>(DatabaseHelper helper, CancellationToken cancellationToken = default) where T : new()
    {
        ArgumentNullException.ThrowIfNull(helper);
        if (_tableName == null)
            throw new InvalidOperationException("Tabela não especificada");
        return helper.ExecuteQueryStreamAsync<T>(this, cancellationToken);
    }

    /// <summary>
    /// Executa o SELECT com paginação estruturada e retorna um <see cref="PagedResult{T}"/>
    /// contendo os itens da página e metadados de navegação (total, páginas, HasNextPage, etc.).
    /// </summary>
    /// <typeparam name="T">Tipo do modelo (deve ter construtor sem parâmetros e propriedades mapeáveis).</typeparam>
    /// <param name="connection">Conexão MySQL.</param>
    /// <param name="request">Requisição de página (<see cref="PageRequest"/>) com <c>Page</c> e <c>PageSize</c>.</param>
    /// <param name="cancellationToken">Token de cancelamento cooperativo.</param>
    /// <returns>Resultado paginado contendo os itens e metadados.</returns>
    public Task<PagedResult<T>> PaginateAsync<T>(MySQL connection, PageRequest request, CancellationToken cancellationToken = default) where T : new()
    {
        ArgumentNullException.ThrowIfNull(connection);
        if (_tableName == null)
            throw new InvalidOperationException("Tabela não especificada");
        return connection.PaginateAsync<T>(this, request, cancellationToken);
    }

    /// <summary>
    /// Executa o SELECT com paginação estruturada usando um <see cref="DatabaseHelper"/>
    /// (que gerencia a conexão automaticamente).
    /// </summary>
    public Task<PagedResult<T>> PaginateAsync<T>(DatabaseHelper helper, PageRequest request, CancellationToken cancellationToken = default) where T : new()
    {
        ArgumentNullException.ThrowIfNull(helper);
        if (_tableName == null)
            throw new InvalidOperationException("Tabela não especificada");
        return helper.PaginateAsync<T>(this, request, cancellationToken);
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

    public Task<bool> ExistsAsync(MySQL connection, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(connection);
        if (_tableName == null)
            throw new InvalidOperationException("Tabela não especificada");
        return connection.ExecuteExistsAsync(this, cancellationToken);
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

    public SelectQueryBuilder Distinct()
    {
        _distinct = true;
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

    public SelectQueryBuilder Where(string field, object value, QueryOperator op)
    {
        return Where(field, value, op.ToSqlString());
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

    public SelectQueryBuilder OrWhere(string field, object value, QueryOperator op)
    {
        return OrWhere(field, value, op.ToSqlString());
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

    public SelectQueryBuilder WhereIn(string field, SelectQueryBuilder subquery)
    {
        ArgumentNullException.ThrowIfNull(subquery);
        _whereConditions.Add(new WhereCondition { Field = ResolveField(field), Operator = "IN", Logic = "AND", Subquery = subquery });
        return this;
    }

    public SelectQueryBuilder WhereNotIn<T>(string field, IEnumerable<T> values)
    {
        _whereConditions.Add(new WhereCondition { Field = ResolveField(field), Values = MaterializeValues(values), Operator = "NOT IN", Logic = "AND" });
        return this;
    }

    public SelectQueryBuilder OrWhereIn<T>(string field, IEnumerable<T> values)
    {
        _whereConditions.Add(new WhereCondition { Field = ResolveField(field), Values = MaterializeValues(values), Operator = "IN", Logic = "OR" });
        return this;
    }

    public SelectQueryBuilder OrWhereNotIn<T>(string field, IEnumerable<T> values)
    {
        _whereConditions.Add(new WhereCondition { Field = ResolveField(field), Values = MaterializeValues(values), Operator = "NOT IN", Logic = "OR" });
        return this;
    }

    public SelectQueryBuilder WhereNotIn(string field, SelectQueryBuilder subquery)
    {
        ArgumentNullException.ThrowIfNull(subquery);
        _whereConditions.Add(new WhereCondition { Field = ResolveField(field), Operator = "NOT IN", Logic = "AND", Subquery = subquery });
        return this;
    }

    public SelectQueryBuilder WhereExists(SelectQueryBuilder subquery)
    {
        ArgumentNullException.ThrowIfNull(subquery);
        _whereConditions.Add(new WhereCondition { Operator = "EXISTS", Logic = "AND", Subquery = subquery });
        return this;
    }

    public SelectQueryBuilder WhereNotExists(SelectQueryBuilder subquery)
    {
        ArgumentNullException.ThrowIfNull(subquery);
        _whereConditions.Add(new WhereCondition { Operator = "NOT EXISTS", Logic = "AND", Subquery = subquery });
        return this;
    }

    public SelectQueryBuilder OrWhereExists(SelectQueryBuilder subquery)
    {
        ArgumentNullException.ThrowIfNull(subquery);
        _whereConditions.Add(new WhereCondition { Operator = "EXISTS", Logic = "OR", Subquery = subquery });
        return this;
    }

    public SelectQueryBuilder OrWhereNotExists(SelectQueryBuilder subquery)
    {
        ArgumentNullException.ThrowIfNull(subquery);
        _whereConditions.Add(new WhereCondition { Operator = "NOT EXISTS", Logic = "OR", Subquery = subquery });
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

    public SelectQueryBuilder GroupBy(params string[] fields)
    {
        foreach (var field in fields)
        {
            _groupBys.Add(ResolveField(field));
        }

        return this;
    }

    public SelectQueryBuilder Having(string field, object value, string op = "=")
    {
        ValidateOperator(op);
        _havingConditions.Add(new WhereCondition { Field = ResolveField(field), Value = value, Operator = op, Logic = "AND" });
        return this;
    }

    public SelectQueryBuilder Having(string field, object value, QueryOperator op)
    {
        return Having(field, value, op.ToSqlString());
    }

    public SelectQueryBuilder HavingRaw(string sql, params object[] parameters)
    {
        _havingConditions.Add(new WhereCondition { RawSql = sql, RawParameters = parameters, Operator = "RAW", Logic = "AND" });
        return this;
    }

    public SelectQueryBuilder OrHaving(string field, object value, string op = "=")
    {
        ValidateOperator(op);
        _havingConditions.Add(new WhereCondition { Field = ResolveField(field), Value = value, Operator = op, Logic = "OR" });
        return this;
    }

    public SelectQueryBuilder OrHaving(string field, object value, QueryOperator op)
    {
        return OrHaving(field, value, op.ToSqlString());
    }

    public SelectQueryBuilder OrHavingRaw(string sql, params object[] parameters)
    {
        _havingConditions.Add(new WhereCondition { RawSql = sql, RawParameters = parameters, Operator = "RAW", Logic = "OR" });
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

    public (string Sql, MySqlCommand Command) BuildCount()
    {
        _paramCounter = 0;
        if (string.IsNullOrEmpty(_tableName))
            throw new InvalidOperationException("Nome da tabela não definido");

        if (_groupBys.Count == 0 && _havingConditions.Count == 0 && !_distinct)
            return BuildQuery("COUNT(*)", includeOrderBy: false, limit: null, offset: null);

        var (innerSql, innerCommand) = BuildQuery(BuildSelectionClause(), includeOrderBy: false, limit: null, offset: null);
        var command = new MySqlCommand();
        RebindParameters(innerCommand, command, innerSql, out var rewrittenSql);
        var sql = $"SELECT COUNT(*) FROM ({rewrittenSql}) AS `count_query`";
        command.CommandText = sql;
        return (sql, command);
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
        AppendGroupByAndHaving(sqlBuilder, command);
        sqlBuilder.Append(" LIMIT 1)");

        var sql = sqlBuilder.ToString();
        command.CommandText = sql;
        return (sql, command);
    }

    protected static string GetTableName<T>()
    {
        var attr = (DbTableAttribute?)Attribute.GetCustomAttribute(typeof(T), typeof(DbTableAttribute));
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

    public SelectQueryBuilder Clone()
    {
        var clone = (SelectQueryBuilder)MemberwiseClone();
        clone._fields = _fields.Select(field => new SelectionField { Name = field.Name, IsRaw = field.IsRaw }).ToList();
        clone._joins = _joins.Select(join => new JoinClause
        {
            Table = join.Table,
            First = join.First,
            Operator = join.Operator,
            Second = join.Second,
            Type = join.Type
        }).ToList();
        clone._whereConditions = _whereConditions.Select(CloneWhereCondition).ToList();
        clone._havingConditions = _havingConditions.Select(CloneWhereCondition).ToList();
        clone._groupBys = new List<string>(_groupBys);
        clone._orderBys = new List<string>(_orderBys);
        return clone;
    }

    private string BuildWhereClause(WhereCondition condition, MySqlCommand command)
    {
        if (condition.Operator == "RAW")
        {
            string sql = condition.RawSql ?? string.Empty;
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
            case "EXISTS":
            case "NOT EXISTS":
                return BuildExistsClause(condition, command);

            case "IS NULL":
            case "IS NOT NULL":
                return $"{escapedField} {condition.Operator}";

            case "IN":
            case "NOT IN":
                if (condition.Subquery != null)
                    return BuildInSubqueryClause(condition, command, escapedField);

                var values = condition.Values ?? new List<object>();
                var inParams = new List<string>(values.Count);
                foreach (var val in values)
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

    private string BuildInSubqueryClause(WhereCondition condition, MySqlCommand command, string escapedField)
    {
        var (_, subqueryCommand) = condition.Subquery!.Build();
        RebindParameters(subqueryCommand, command, subqueryCommand.CommandText, out var rewrittenSql);
        return $"{escapedField} {condition.Operator} ({rewrittenSql})";
    }

    private string BuildExistsClause(WhereCondition condition, MySqlCommand command)
    {
        var subquery = condition.Subquery ?? throw new InvalidOperationException("A subquery do EXISTS não foi informada.");
        var (_, subqueryCommand) = subquery.Build();
        RebindParameters(subqueryCommand, command, subqueryCommand.CommandText, out var rewrittenSql);
        return $"{condition.Operator} ({rewrittenSql})";
    }

    private void RebindParameters(MySqlCommand sourceCommand, MySqlCommand targetCommand, string sourceSql, out string rewrittenSql)
    {
        rewrittenSql = sourceSql;

        foreach (MySqlParameter parameter in sourceCommand.Parameters)
        {
            var nextName = GetNextParamName();
            var newParameterName = $"@{nextName}";
            rewrittenSql = Regex.Replace(rewrittenSql, $@"{Regex.Escape(parameter.ParameterName)}(?!\w)", newParameterName);
            AddParameter(targetCommand, nextName, parameter.Value);
        }
    }

    private static WhereCondition CloneWhereCondition(WhereCondition condition)
    {
        return new WhereCondition
        {
            Field = condition.Field,
            Value = condition.Value,
            SecondValue = condition.SecondValue,
            Values = condition.Values == null ? null : new List<object>(condition.Values),
            Operator = condition.Operator,
            Logic = condition.Logic,
            RawSql = condition.RawSql,
            RawParameters = condition.RawParameters == null ? null : (object[])condition.RawParameters.Clone(),
            Subquery = condition.Subquery?.Clone()
        };
    }

    private static string ReplaceParameters(string sql, MySqlParameterCollection parameters)
    {
        var debugSql = sql;

        foreach (var parameter in parameters
                     .OrderByDescending(p => p.ParameterName.Length))
        {
            if (parameter.Value != null)
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
        if (_distinct)
        {
            sqlBuilder.Append("DISTINCT ");
        }
        sqlBuilder.Append(selection);
        sqlBuilder.Append(" FROM ");
        sqlBuilder.Append(EscapeIdentifier(_tableName));
        AppendJoinsAndWhere(sqlBuilder, command);
        AppendGroupByAndHaving(sqlBuilder, command);

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
                var clause = BuildWhereClause(condition, command);
                whereClauses.Add(i > 0 ? $"{logic} {clause}" : clause);
            }

            sqlBuilder.Append(" WHERE ");
            sqlBuilder.Append(string.Join(" ", whereClauses));
        }
    }

    private void AppendGroupByAndHaving(StringBuilder sqlBuilder, MySqlCommand command)
    {
        if (_groupBys.Count > 0)
        {
            sqlBuilder.Append(" GROUP BY ");
            sqlBuilder.Append(string.Join(", ", _groupBys.Select(EscapeIdentifier)));
        }

        if (_havingConditions.Count <= 0) return;
        var havingClauses = new List<string>(_havingConditions.Count);
        for (var i = 0; i < _havingConditions.Count; i++)
        {
            var condition = _havingConditions[i];
            var logic = i > 0 ? condition.Logic : "";
            var clause = BuildWhereClause(condition, command);
            havingClauses.Add(i > 0 ? $"{logic} {clause}" : clause);
        }

        sqlBuilder.Append(" HAVING ");
        sqlBuilder.Append(string.Join(" ", havingClauses));
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
        if (values is not ICollection<T> collection) return values.Cast<object>().ToList();
        var result = new List<object>(collection.Count);
        result.AddRange(collection.Cast<object>());
        return result;
    }

    private static void AddParameter(MySqlCommand command, string paramName, object? value)
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
        public string Name { get; set; } = string.Empty;
        public bool IsRaw { get; set; }
    }

    private class JoinClause
    {
        public string? Table { get; set; }
        public string? First { get; set; }
        public string? Operator { get; set; }
        public string? Second { get; set; }
        public string? Type { get; set; }
    }

    private class WhereCondition
    {
        public string? Field { get; set; }
        public object? Value { get; set; }
        public object? SecondValue { get; set; }
        public List<object>? Values { get; set; }
        public string? Operator { get; set; }
        public string? Logic { get; set; }
        public string? RawSql { get; set; }
        public object[]? RawParameters { get; set; }
        public SelectQueryBuilder? Subquery { get; set; }
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

    public SelectQueryBuilder<T> Where(Expression<Func<T, bool>> predicate)
    {
        var translated = WhereExpressionTranslator.BuildRaw(predicate, ResolveField);
        base.WhereRaw(translated.Sql, translated.Parameters);

        return this;
    }

    protected override string ResolveField(string field)
    {
        return _fieldMapping.TryGetValue(field, out var column) ? column : field;
    }
}

public class SelectQueryExecutor
{
    private readonly MySqlConnection _connection;
    private readonly MySqlTransaction? _transaction;

    public SelectQueryExecutor(MySqlConnection connection) => _connection = connection;
    public SelectQueryExecutor(MySqlConnection connection, MySqlTransaction transaction)
    {
        _connection = connection;
        _transaction = transaction;
    }

    public async Task<MySqlReader> ExecuteQueryAsync(SelectQueryBuilder builder)
    {
        var (_, command) = builder.Build();
        command.Connection = _connection;
        if (_transaction != null) command.Transaction = _transaction;
        return new MySqlReader(await command.ExecuteReaderAsync());
    }

    public MySqlReader ExecuteQuerySync(SelectQueryBuilder builder)
    {
        var (_, command) = builder.Build();
        command.Connection = _connection;
        if (_transaction != null) command.Transaction = _transaction;
        return new MySqlReader(command.ExecuteReader());
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
