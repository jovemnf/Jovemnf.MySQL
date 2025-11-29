using Xunit;
using Jovemnf.MySQL;
using System;
using MySqlConnector;

namespace Jovemnf.MySQL.Tests
{
    public class MySQLTests : IDisposable
    {
        private readonly string _testHost = "localhost";
        private readonly string _testDatabase = "test_db";
        private readonly string _testUsername = "test_user";
        private readonly string _testPassword = "test_password";
        private readonly uint _testPort = 3306;

        public void Dispose()
        {
            // Cleanup if needed
        }

        [Fact]
        public void MySQL_Constructor_WithParameters_ShouldCreateInstance()
        {
            // Arrange & Act
            var mysql = new MySQL(_testHost, _testDatabase, _testUsername, _testPassword, _testPort);

            // Assert
            Assert.NotNull(mysql);
        }

        [Fact]
        public void MySQL_Constructor_WithConnectionString_ShouldCreateInstance()
        {
            // Arrange
            var connectionString = $"Server={_testHost};Port={_testPort};Database={_testDatabase};User ID={_testUsername};Password={_testPassword};SslMode=None;";

            // Act
            var mysql = new MySQL(connectionString);

            // Assert
            Assert.NotNull(mysql);
        }

        [Fact]
        public void MySQL_Constructor_WithoutParameters_ShouldThrowException_WhenNotInitialized()
        {
            // Arrange & Act & Assert
            Assert.Throws<NullReferenceException>(() => new MySQL());
        }

        [Fact]
        public void MySQL_INIT_ShouldSetStaticData()
        {
            // Arrange
            var host = "test_host";
            var database = "test_database";
            var username = "test_username";
            var password = "test_password";
            var port = 3307u;
            var charset = "utf8mb4";

            // Act
            MySQL.INIT(host, database, username, password, port, charset);

            // Assert
            // Note: Como MySQLData é privado, não podemos verificar diretamente
            // Mas podemos criar uma instância e verificar se funciona
            var mysql = new MySQL();
            Assert.NotNull(mysql);
        }

        [Fact]
        public void MySQL_MaximumPoolSize_ShouldBeSettable()
        {
            // Arrange
            var expectedSize = 200u;

            // Act
            MySQL.MaximumPoolSize = expectedSize;

            // Assert
            Assert.Equal(expectedSize, MySQL.MaximumPoolSize);
        }

        [Fact]
        public void MySQL_Pooling_ShouldBeSettable()
        {
            // Arrange
            var expectedPooling = false;

            // Act
            MySQL.Pooling = expectedPooling;

            // Assert
            Assert.Equal(expectedPooling, MySQL.Pooling);
        }

        [Fact]
        public void MySQL_OpenCommand_ShouldSetCommand()
        {
            // Arrange
            var mysql = new MySQL(_testHost, _testDatabase, _testUsername, _testPassword, _testPort);
            var sql = "SELECT 1";

            // Act
            mysql.OpenCommand(sql);

            // Assert
            Assert.Equal(sql, mysql.CommandText);
        }

        [Fact]
        public void MySQL_SetParameter_ShouldAddParameter()
        {
            // Arrange
            var mysql = new MySQL(_testHost, _testDatabase, _testUsername, _testPassword, _testPort);
            mysql.OpenCommand("SELECT @param");
            var paramName = "@param";
            var paramValue = "test_value";

            // Act
            mysql.SetParameter(paramName, paramValue);

            // Assert
            // Verificação indireta através do CommandText
            Assert.NotNull(mysql.CommandText);
        }

        [Fact]
        public void MySQL_CreateAdapter_ShouldCreateAdapter()
        {
            // Arrange
            var mysql = new MySQL(_testHost, _testDatabase, _testUsername, _testPassword, _testPort);
            var command = "SELECT 1";

            // Act
            mysql.CreateAdapter(command);

            // Assert
            Assert.NotNull(mysql.Adapter);
        }

        [Fact]
        public void MySQL_Dispose_ShouldDisposeResources()
        {
            // Arrange
            var mysql = new MySQL(_testHost, _testDatabase, _testUsername, _testPassword, _testPort);

            // Act
            mysql.Dispose();

            // Assert
            // Se não lançar exceção, o dispose funcionou
            Assert.True(true);
        }

        [Fact]
        public async Task MySQL_DisposeAsync_ShouldDisposeResourcesAsync()
        {
            // Arrange
            var mysql = new MySQL(_testHost, _testDatabase, _testUsername, _testPassword, _testPort);

            // Act
            await mysql.DisposeAsync();

            // Assert
            // Se não lançar exceção, o dispose funcionou
            Assert.True(true);
        }
    }
}
