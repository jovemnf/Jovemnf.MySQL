using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Reflection;
using MySqlConnector;
using Jovemnf.MySQL.Geometry;

namespace Jovemnf.MySQL.Builder
{
    public class UpdateQueryBuilder
    {
        protected string _tableName;

        public static UpdateQueryBuilder For<T>() => new UpdateQueryBuilder<T>();

        protected virtual string ResolveField(string field) => field;
        private Dictionary<string, object> _fields = new Dictionary<string, object>();
        private List<WhereCondition> _whereConditions = new List<WhereCondition>();
// ... (skip unchanged lines) ...
        private int _paramCounter = 0;
        private bool _allowAll = false;
        
        private static readonly HashSet<string> _allowedOperators = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
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
                Values = values.Cast<object>().ToList(), 
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
                Values = values.Cast<object>().ToList(), 
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
            var setClauses = new List<string>();
            foreach (var field in _fields)
            {
                var paramName = GetNextParamName();
                
                if (field.Value is PointValue pointValue)
                {
                    setClauses.Add($"`{EscapeIdentifier(field.Key)}` = ST_GeomFromWKB(@{paramName}, {pointValue.Point.SRID})");
                    command.Parameters.AddWithValue($"@{paramName}", pointValue.Point.ToWKB());
                }
                else if (field.Value is PolygonValue polygonValue)
                {
                    setClauses.Add($"`{EscapeIdentifier(field.Key)}` = ST_PolygonFromText(@{paramName}, {polygonValue.Polygon.SRID})");
                    command.Parameters.AddWithValue($"@{paramName}", polygonValue.Polygon.ToWKT());
                }
                else
                {
                    setClauses.Add($"`{EscapeIdentifier(field.Key)}` = @{paramName}");
                    command.Parameters.AddWithValue($"@{paramName}", field.Value ?? DBNull.Value);
                }
            }

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

            var sql = $"UPDATE `{EscapeIdentifier(_tableName)}` SET {string.Join(", ", setClauses)}";

            if (whereClauses.Count > 0)
            {
               sql += $" WHERE {string.Join(" ", whereClauses)}";
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

    public class UpdateQueryBuilder<T> : UpdateQueryBuilder
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

        public UpdateQueryBuilder()
        {
            Table(GetTableName<T>());
        }

        protected override string ResolveField(string field)
        {
            return _fieldMapping.TryGetValue(field, out var column) ? column : field;
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
            var (sql, command) = builder.Build();
            command.Connection = _connection;
            
            if (_transaction != null)
                command.Transaction = _transaction;

            return await command.ExecuteNonQueryAsync();
        }

        // Executar síncrono
        public int Execute(UpdateQueryBuilder builder)
        {
            var (sql, command) = builder.Build();
            command.Connection = _connection;
            
            if (_transaction != null)
                command.Transaction = _transaction;

            return command.ExecuteNonQuery();
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
            var startTime = DateTime.Now;
            
            try
            {
                var rowsAffected = await ExecuteAsync(builder);
                var (sql, _) = builder.Build();
                
                return new UpdateResult
                {
                    Success = true,
                    RowsAffected = rowsAffected,
                    Sql = sql,
                    ExecutionTime = DateTime.Now - startTime
                };
            }
            catch (Exception ex)
            {
                var (sql, _) = builder.Build();
                
                return new UpdateResult
                {
                    Success = false,
                    RowsAffected = 0,
                    Sql = sql,
                    ExecutionTime = DateTime.Now - startTime,
                    Error = ex.Message,
                    Exception = ex
                };
            }
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
}

// Helper para gerenciar conexões
namespace Jovemnf.MySQL
{
    using Jovemnf.MySQL.Builder;

    public class DatabaseHelper
    {
        private readonly string _connectionString;

        public DatabaseHelper(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<int> ExecuteUpdateAsync(UpdateQueryBuilder builder)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();
            
            var executor = new UpdateQueryExecutor(connection);
            return await executor.ExecuteAsync(builder);
        }

        public async Task<int> ExecuteDeleteAsync(DeleteQueryBuilder builder)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();
            
            var executor = new DeleteQueryExecutor(connection);
            return await executor.ExecuteAsync(builder);
        }

        public int ExecuteDeleteSync(DeleteQueryBuilder builder)
        {
            using var connection = new MySqlConnection(_connectionString);
            connection.Open();
            
            var executor = new DeleteQueryExecutor(connection);
            var (sql, command) = builder.Build();
            command.Connection = connection;
            return command.ExecuteNonQuery();
        }

        // Executar insert com connection automática
        public async Task<long> ExecuteInsertAsync(InsertQueryBuilder builder, bool lastID = true)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();
            
            var executor = new InsertQueryExecutor(connection);
            return await executor.ExecuteAsync(builder, lastID);
        }

        // Executar select com connection automática
        public async Task<MySQLReader> ExecuteQueryAsync(SelectQueryBuilder builder)
        {
            var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();
            
            var executor = new SelectQueryExecutor(connection);
            return await executor.ExecuteQueryAsync(builder);
        }

        // Executar update com transação
        public async Task<int> ExecuteUpdateWithTransactionAsync(
            Func<MySqlConnection, MySqlTransaction, Task<UpdateQueryBuilder>> builderFunc)
        {
            await using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var transaction = await connection.BeginTransactionAsync();
            
            try
            {
                var builder = await builderFunc(connection, transaction);
                var executor = new UpdateQueryExecutor(connection, transaction);
                var result = await executor.ExecuteAsync(builder);
                
                await transaction.CommitAsync();
                return result;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // Executar múltiplos updates em uma transação
        public async Task<List<int>> ExecuteMultipleUpdatesAsync(
            params UpdateQueryBuilder[] builders)
        {
            await using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var transaction = await connection.BeginTransactionAsync();
            
            try
            {
                var results = new List<int>();
                var executor = new UpdateQueryExecutor(connection, transaction);
                
                foreach (var builder in builders)
                {
                    var result = await executor.ExecuteAsync(builder);
                    results.Add(result);
                }
                
                await transaction.CommitAsync();
                return results;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}