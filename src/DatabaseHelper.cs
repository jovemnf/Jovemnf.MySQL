using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Jovemnf.MySQL.Builder;
using MySqlConnector;

namespace Jovemnf.MySQL
{
    public partial class DatabaseHelper
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

        public async Task<int> ExecuteInsertBatchAsync(InsertBatchQueryBuilder builder)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            var executor = new InsertBatchQueryExecutor(connection);
            return await executor.ExecuteAsync(builder);
        }

        // Executar select com connection automática
        public async Task<MySqlReader> ExecuteQueryAsync(SelectQueryBuilder builder)
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
