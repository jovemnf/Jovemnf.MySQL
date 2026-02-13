using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MySqlConnector;

namespace Jovemnf.MySQL.Builder
{
    public class DeleteQueryBuilder
    {
        private string _tableName;
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
        
        public DeleteQueryBuilder From(string tableName) => Table(tableName);

        public DeleteQueryBuilder Where(string field, object value, string op = "=")
        {
            ValidateOperator(op);
            _whereConditions.Add(new WhereCondition 
            { 
                Field = field, 
                Value = value, 
                Operator = op,
                Logic = "AND"
            });
            return this;
        }

        public DeleteQueryBuilder Where(string field, object value, QueryOperator op)
        {
            return Where(field, value, op.ToSqlString());
        }

        public DeleteQueryBuilder WhereIn<T>(string field, IEnumerable<T> values)
        {
            _whereConditions.Add(new WhereCondition 
            { 
                Field = field, 
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
                Field = field, 
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
                Field = field, 
                Value = value, 
                Operator = op,
                Logic = "OR"
            });
            return this;
        }
        
        public DeleteQueryBuilder OrWhere(string field, object value, QueryOperator op)
        {
            return OrWhere(field, value, op.ToSqlString());
        }

        public DeleteQueryBuilder WhereNull(string field)
        {
            _whereConditions.Add(new WhereCondition 
            { 
                Field = field, 
                Operator = "IS NULL",
                Logic = "AND"
            });
            return this;
        }

        public DeleteQueryBuilder WhereNotNull(string field)
        {
            _whereConditions.Add(new WhereCondition 
            { 
                Field = field, 
                Operator = "IS NOT NULL",
                Logic = "AND"
            });
            return this;
        }

        public DeleteQueryBuilder WhereBetween(string field, object start, object end)
        {
            _whereConditions.Add(new WhereCondition 
            { 
                Field = field,
                Value = start,
                SecondValue = end,
                Operator = "BETWEEN",
                Logic = "AND"
            });
            return this;
        }

        public DeleteQueryBuilder WhereLike(string field, string pattern)
        {
            _whereConditions.Add(new WhereCondition 
            { 
                Field = field,
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
