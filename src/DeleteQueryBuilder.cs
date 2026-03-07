using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using MySqlConnector;

namespace Jovemnf.MySQL.Builder
{
    public class DeleteQueryBuilder
    {
        protected string _tableName;
        private string _dateTimeSourceTimeZone;
        private string _dateTimeTargetTimeZone;

        public static DeleteQueryBuilder For<T>() => new DeleteQueryBuilder<T>();

        protected virtual string ResolveField(string field) => field;
        private List<WhereCondition> _whereConditions = new List<WhereCondition>();
        private int _paramCounter = 0;
        private bool _allowAll = false;
        private int? _limit;

        private static readonly HashSet<string> _allowedOperators = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "=", "<>", "!=", "<", "<=", ">", ">=", "LIKE", "NOT LIKE", "IN", "NOT IN", "IS NULL", "IS NOT NULL", "BETWEEN", "REGEXP", "NOT REGEXP"
        };
        
        /// <summary>
        /// Permite que o delete seja executado em todas as linhas (sem WHERE).
        /// </summary>
        public DeleteQueryBuilder All()
        {
            _allowAll = true;
            return this;
        }

        public DeleteQueryBuilder Table(string tableName)
        {
            _tableName = tableName;
            return this;
        }
        
        public Task ExecuteAsync(MySQL connection)
        {
            ArgumentNullException.ThrowIfNull(connection);
            if (_tableName == null) 
                throw new InvalidOperationException("Tabela não especificada");
            return connection.ExecuteDeleteAsync(this);
        }

        /// <summary>
        /// Executa o DELETE e retorna as linhas que foram removidas mapeadas para o tipo T (SELECT antes do DELETE).
        /// Não suportado quando All() foi usado; nesse caso use ExecuteAsync(connection).
        /// </summary>
        /// <typeparam name="T">Tipo do modelo (deve ter construtor sem parâmetros e propriedades mapeáveis).</typeparam>
        /// <param name="connection">Conexão MySQL.</param>
        /// <returns>Lista das entidades que foram deletadas.</returns>
        public Task<List<T>> ExecuteAsync<T>(MySQL connection) where T : new()
        {
            ArgumentNullException.ThrowIfNull(connection);
            if (_tableName == null) 
                throw new InvalidOperationException("Tabela não especificada");
            return connection.ExecuteDeleteAsync<T>(this);
        }

        /// <summary>
        /// Constrói um SELECT * para as mesmas condições WHERE do DELETE (usado por ExecuteAsync&lt;T&gt;).
        /// </summary>
        internal (string Sql, MySqlCommand Command) BuildSelect()
        {
            if (_whereConditions.Count == 0 && !_allowAll)
                throw new InvalidOperationException("Nenhuma condição WHERE definida para SELECT.");
            if (_allowAll)
                throw new InvalidOperationException("ExecuteAsync<T> não é suportado quando All() foi usado. Use ExecuteAsync(connection).");

            var command = new MySqlCommand();
            var savedCounter = _paramCounter;
            _paramCounter = 0;

            var whereClauses = new List<string>();
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

        public DeleteQueryBuilder From(string tableName) => Table(tableName);

        public DeleteQueryBuilder Where(string field, object value, string op = "=")
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

        public DeleteQueryBuilder UseDateTimeTimeZone(string sourceTimeZone, string targetTimeZone)
        {
            _dateTimeSourceTimeZone = MySqlSessionTimeZone.Normalize(sourceTimeZone);
            _dateTimeTargetTimeZone = MySqlSessionTimeZone.Normalize(targetTimeZone);
            return this;
        }

        public DeleteQueryBuilder WhereDateTime(string field, DateTime value, string op = "=")
        {
            return Where(field, ConvertDateTimeValue(value), op);
        }

        public DeleteQueryBuilder Where(string field, object value, QueryOperator op)
        {
            return Where(field, value, op.ToSqlString());
        }

        public DeleteQueryBuilder WhereIn<T>(string field, IEnumerable<T> values)
        {
            _whereConditions.Add(new WhereCondition 
            { 
                Field = ResolveField(field), 
                Values = values.Cast<object>().ToList(), 
                Operator = "IN",
                Logic = "AND"
            });
            return this;
        }

        public DeleteQueryBuilder WhereNotIn<T>(string field, IEnumerable<T> values)
        {
            _whereConditions.Add(new WhereCondition 
            { 
                Field = ResolveField(field), 
                Values = values.Cast<object>().ToList(), 
                Operator = "NOT IN",
                Logic = "AND"
            });
            return this;
        }

        public DeleteQueryBuilder OrWhere(string field, object value, string op = "=")
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

        public DeleteQueryBuilder OrWhereDateTime(string field, DateTime value, string op = "=")
        {
            return OrWhere(field, ConvertDateTimeValue(value), op);
        }
        
        public DeleteQueryBuilder OrWhere(string field, object value, QueryOperator op)
        {
            return OrWhere(field, value, op.ToSqlString());
        }

        public DeleteQueryBuilder WhereNull(string field)
        {
            _whereConditions.Add(new WhereCondition 
            { 
                Field = ResolveField(field), 
                Operator = "IS NULL",
                Logic = "AND"
            });
            return this;
        }

        public DeleteQueryBuilder WhereNotNull(string field)
        {
            _whereConditions.Add(new WhereCondition 
            { 
                Field = ResolveField(field), 
                Operator = "IS NOT NULL",
                Logic = "AND"
            });
            return this;
        }

        public DeleteQueryBuilder WhereBetween(string field, object start, object end)
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

        public DeleteQueryBuilder WhereBetweenDateTime(string field, DateTime start, DateTime end)
        {
            return WhereBetween(field, ConvertDateTimeValue(start), ConvertDateTimeValue(end));
        }

        public DeleteQueryBuilder WhereLike(string field, string pattern)
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
        
        public DeleteQueryBuilder Limit(int limit)
        {
            _limit = limit;
            return this;
        }

        public (string Sql, MySqlCommand Command) Build()
        {
            _paramCounter = 0;
            if (string.IsNullOrEmpty(_tableName))
                throw new InvalidOperationException("Nome da tabela não definido");
            
            if (_whereConditions.Count == 0 && !_allowAll)
                throw new InvalidOperationException("Nenhuma condição WHERE definida. Use .All() se realmente deseja deletar todas as linhas.");

            var command = new MySqlCommand();
            
            // WHERE clause
            var whereClauses = new List<string>();
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

            var sql = $"DELETE FROM `{EscapeIdentifier(_tableName)}`";
            
            if (whereClauses.Count > 0)
            {
               sql += $" WHERE {string.Join(" ", whereClauses)}";
            }
            
            if (_limit.HasValue)
            {
                sql += $" LIMIT {_limit.Value}";
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

        private string EscapeIdentifier(string identifier)
        {
            if (string.IsNullOrEmpty(identifier)) return identifier;
            return identifier.Replace("`", "``");
        }

        private void ValidateOperator(string op)
        {
            if (!_allowedOperators.Contains(op))
            {
                throw new ArgumentException($"Operador não permitido: {op}");
            }
        }
        
        private string BuildWhereClause(WhereCondition condition, MySqlCommand command)
        {
            switch (condition.Operator)
            {
                case "IS NULL":
                case "IS NOT NULL":
                    return $"`{EscapeIdentifier(condition.Field)}` {condition.Operator}";

                case "IN":
                case "NOT IN":
                    var inParams = new List<string>();
                    foreach (var val in condition.Values)
                    {
                        var nextParamName = GetNextParamName();
                        inParams.Add($"@{nextParamName}");
                        command.Parameters.AddWithValue($"@{nextParamName}", val ?? DBNull.Value);
                    }
                    return $"`{EscapeIdentifier(condition.Field)}` {condition.Operator} ({string.Join(", ", inParams)})";

                case "BETWEEN":
                    var startParam = GetNextParamName();
                    var endParam = GetNextParamName();
                    command.Parameters.AddWithValue($"@{startParam}", condition.Value ?? DBNull.Value);
                    command.Parameters.AddWithValue($"@{endParam}", condition.SecondValue ?? DBNull.Value);
                    return $"`{EscapeIdentifier(condition.Field)}` BETWEEN @{startParam} AND @{endParam}";

                default:
                    var paramName = GetNextParamName();
                    command.Parameters.AddWithValue($"@{paramName}", condition.Value ?? DBNull.Value);
                    return $"`{EscapeIdentifier(condition.Field)}` {condition.Operator} @{paramName}";
            }
        }

        private string GetNextParamName()
        {
            return $"p{_paramCounter++}";
        }

        private DateTime ConvertDateTimeValue(DateTime value)
        {
            return DateTimeTimeZoneConverter.Convert(value, _dateTimeSourceTimeZone, _dateTimeTargetTimeZone);
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
    }

    public class DeleteQueryBuilder<T> : DeleteQueryBuilder
    {
        private static readonly Dictionary<string, string> _fieldMapping = CreateFieldMapping();

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

        public DeleteQueryBuilder()
        {
            Table(GetTableName<T>());
        }

        protected override string ResolveField(string field)
        {
            return _fieldMapping.TryGetValue(field, out var column) ? column : field;
        }
    }
    
    public class DeleteQueryExecutor
    {
        private readonly MySqlConnection _connection;
        private MySqlTransaction _transaction;

        public DeleteQueryExecutor(MySqlConnection connection)
        {
            _connection = connection;
        }

        public DeleteQueryExecutor(MySqlConnection connection, MySqlTransaction transaction)
        {
            _connection = connection;
            _transaction = transaction;
        }

        public async Task<int> ExecuteAsync(DeleteQueryBuilder builder)
        {
            var (sql, command) = builder.Build();
            command.Connection = _connection;
            
            if (_transaction != null)
                command.Transaction = _transaction;

            return await command.ExecuteNonQueryAsync();
        }
    }
}
