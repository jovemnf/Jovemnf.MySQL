using Xunit;
using Jovemnf.MySQL;
using System;
using System.Threading.Tasks;

namespace Jovemnf.MySQL.Tests
{
    /// <summary>
    /// Testes de integração que requerem um banco de dados MySQL configurado.
    /// Estes testes são opcionais e podem ser ignorados se não houver um banco de dados disponível.
    /// Para executar estes testes, configure as variáveis de ambiente ou ajuste as credenciais abaixo.
    /// </summary>
    public class MySQLIntegrationTests : IDisposable
    {
        private readonly string _testHost;
        private readonly string _testDatabase;
        private readonly string _testUsername;
        private readonly string _testPassword;
        private readonly uint _testPort;
        private MySQL? _mysql;

        public MySQLIntegrationTests()
        {
            // Configurar credenciais do banco de dados de teste
            // Você pode usar variáveis de ambiente ou ajustar aqui
            _testHost = Environment.GetEnvironmentVariable("MYSQL_TEST_HOST") ?? "localhost";
            _testDatabase = Environment.GetEnvironmentVariable("MYSQL_TEST_DATABASE") ?? "test_db";
            _testUsername = Environment.GetEnvironmentVariable("MYSQL_TEST_USERNAME") ?? "test_user";
            _testPassword = Environment.GetEnvironmentVariable("MYSQL_TEST_PASSWORD") ?? "test_password";
            _testPort = uint.TryParse(Environment.GetEnvironmentVariable("MYSQL_TEST_PORT"), out var port) ? port : 3306;
        }

        public void Dispose()
        {
            _mysql?.Dispose();
        }

        [Fact(Skip = "Requer banco de dados MySQL configurado")]
        public async Task MySQL_OpenAsync_ShouldOpenConnection()
        {
            // Arrange
            _mysql = new MySQL(_testHost, _testDatabase, _testUsername, _testPassword, _testPort);

            // Act
            await _mysql.OpenAsync();

            // Assert
            // Se não lançar exceção, a conexão foi aberta com sucesso
            Assert.True(true);
        }

        [Fact(Skip = "Requer banco de dados MySQL configurado")]
        public async Task MySQL_ExecuteQueryAsync_ShouldReturnReader()
        {
            // Arrange
            _mysql = new MySQL(_testHost, _testDatabase, _testUsername, _testPassword, _testPort);
            await _mysql.OpenAsync();
            _mysql.OpenCommand("SELECT 1 as test_value");

            // Act
            var reader = await _mysql.ExecuteQueryAsync();

            // Assert
            Assert.NotNull(reader);
            
            if (reader.Read())
            {
                var value = reader.GetInteger("test_value");
                Assert.Equal(1, value);
            }

            reader.Dispose();
        }

        [Fact(Skip = "Requer banco de dados MySQL configurado")]
        public async Task MySQL_ExecuteInsertAsync_ShouldReturnLastInsertId()
        {
            // Arrange
            _mysql = new MySQL(_testHost, _testDatabase, _testUsername, _testPassword, _testPort);
            await _mysql.OpenAsync();
            
            // Criar tabela de teste se não existir
            _mysql.OpenCommand(@"
                CREATE TABLE IF NOT EXISTS test_table (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    name VARCHAR(100)
                )");
            await _mysql.ExecuteUpdateAsync();

            // Inserir registro
            _mysql.OpenCommand("INSERT INTO test_table (name) VALUES (@name)");
            _mysql.SetParameter("@name", "Test Name");

            // Act
            var lastId = await _mysql.ExecuteInsertAsync();

            // Assert
            Assert.True(lastId > 0);

            // Cleanup
            _mysql.OpenCommand("DROP TABLE IF EXISTS test_table");
            await _mysql.ExecuteUpdateAsync();
        }

        [Fact(Skip = "Requer banco de dados MySQL configurado")]
        public async Task MySQL_Transaction_ShouldCommit()
        {
            // Arrange
            _mysql = new MySQL(_testHost, _testDatabase, _testUsername, _testPassword, _testPort);
            await _mysql.OpenAsync();
            await _mysql.BeginAsync();

            // Criar tabela de teste
            _mysql.OpenCommand(@"
                CREATE TABLE IF NOT EXISTS test_transaction (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    value VARCHAR(100)
                )");
            await _mysql.ExecuteUpdateAsync();

            // Inserir registro
            _mysql.OpenCommand("INSERT INTO test_transaction (value) VALUES (@value)");
            _mysql.SetParameter("@value", "Test Value");
            await _mysql.ExecuteInsertAsync();

            // Act
            await _mysql.CommitAsync();

            // Assert
            // Verificar se o registro foi inserido
            _mysql.OpenCommand("SELECT COUNT(*) as count FROM test_transaction");
            var reader = await _mysql.ExecuteQueryAsync();
            if (reader.Read())
            {
                var count = reader.GetInteger("count");
                Assert.True(count > 0);
            }
            reader.Dispose();

            // Cleanup
            _mysql.OpenCommand("DROP TABLE IF EXISTS test_transaction");
            await _mysql.ExecuteUpdateAsync();
        }

        [Fact(Skip = "Requer banco de dados MySQL configurado")]
        public async Task MySQL_Transaction_ShouldRollback()
        {
            // Arrange
            _mysql = new MySQL(_testHost, _testDatabase, _testUsername, _testPassword, _testPort);
            await _mysql.OpenAsync();
            await _mysql.BeginAsync();

            // Criar tabela de teste
            _mysql.OpenCommand(@"
                CREATE TABLE IF NOT EXISTS test_rollback (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    value VARCHAR(100)
                )");
            await _mysql.ExecuteUpdateAsync();

            // Inserir registro
            _mysql.OpenCommand("INSERT INTO test_rollback (value) VALUES (@value)");
            _mysql.SetParameter("@value", "Test Value");
            await _mysql.ExecuteInsertAsync();

            // Act
            await _mysql.RollbackAsync();

            // Assert
            // Verificar se o registro NÃO foi inserido (rollback funcionou)
            _mysql.OpenCommand("SELECT COUNT(*) as count FROM test_rollback");
            var reader = await _mysql.ExecuteQueryAsync();
            if (reader.Read())
            {
                var count = reader.GetInteger("count");
                Assert.Equal(0, count);
            }
            reader.Dispose();

            // Cleanup
            _mysql.OpenCommand("DROP TABLE IF EXISTS test_rollback");
            await _mysql.ExecuteUpdateAsync();
        }
    }
}
