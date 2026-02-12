using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MySqlConnector;

namespace Jovemnf.MySQL;

public class InsertQueryBuilder
{
    private string _tableName;
    private Dictionary<string, object> _fields = new Dictionary<string, object>();
    private int _paramCounter = 0;

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
        _fields[field] = value;
        return this;
    }

    public InsertQueryBuilder Values(Dictionary<string, object> fields)
    {
        foreach (var field in fields)
        {
            _fields[field.Key] = field.Value;
        }
        return this;
    }

    public (string Sql, MySqlCommand Command) Build()
    {
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
            paramNames.Add($"@{paramName}");
            command.Parameters.AddWithValue($"@{paramName}", field.Value ?? DBNull.Value);
        }

        var sql = $"INSERT INTO `{EscapeIdentifier(_tableName)}` ({string.Join(", ", columnNames)}) VALUES ({string.Join(", ", paramNames)})";
        command.CommandText = sql;
        
        return (sql, command);
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
