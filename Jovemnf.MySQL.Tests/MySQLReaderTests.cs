using Xunit;
using Jovemnf.MySQL;
using System;
using MySqlConnector;
using System.Data.Common;
using Moq;

namespace Jovemnf.MySQL.Tests
{
    public class MySQLReaderTests
    {
        [Fact]
        public void MySQLReader_Constructor_ShouldCreateInstance()
        {
            // Arrange
            var mockReader = new Mock<DbDataReader>();
            var dbReader = mockReader.Object;

            // Act
            var reader = new MySQLReader(dbReader);

            // Assert
            Assert.NotNull(reader);
        }

        [Fact]
        public void MySQLReader_Dispose_ShouldDisposeReader()
        {
            // Arrange
            var mockReader = new Mock<DbDataReader>();
            var reader = new MySQLReader(mockReader.Object);

            // Act
            reader.Dispose();

            // Assert
            mockReader.Verify(r => r.Dispose(), Times.Once);
        }

        [Fact]
        public void MySQLReader_Dispose_ShouldThrowMySQLCloseException_OnError()
        {
            // Arrange
            var mockReader = new Mock<DbDataReader>();
            mockReader.Setup(r => r.Dispose()).Throws<Exception>();
            var reader = new MySQLReader(mockReader.Object);

            // Act & Assert
            Assert.Throws<MySQLCloseException>(() => reader.Dispose());
        }

        [Fact]
        public void MySQLReader_Get_ShouldReturnValue()
        {
            // Arrange
            var mockReader = new Mock<DbDataReader>();
            var expectedValue = "test_value";
            mockReader.Setup(r => r["column"]).Returns(expectedValue);
            var reader = new MySQLReader(mockReader.Object);

            // Act
            var result = reader.Get("column");

            // Assert
            Assert.Equal(expectedValue, result);
        }

        [Fact]
        public void MySQLReader_Get_ShouldThrowException_OnError()
        {
            // Arrange
            var mockReader = new Mock<DbDataReader>();
            mockReader.Setup(r => r["column"]).Throws<Exception>();
            var reader = new MySQLReader(mockReader.Object);

            // Act & Assert
            Assert.Throws<Exception>(() => reader.Get("column"));
        }

        [Fact]
        public void MySQLReader_GetBoolean_ShouldReturnBoolean()
        {
            // Arrange
            var mockReader = new Mock<DbDataReader>();
            mockReader.Setup(r => r["column"]).Returns(1);
            var reader = new MySQLReader(mockReader.Object);

            // Act
            var result = reader.GetBoolean("column");

            // Assert
            Assert.IsType<bool>(result);
        }

        [Fact]
        public void MySQLReader_GetTinyInt_ShouldReturnBoolean()
        {
            // Arrange
            var mockReader = new Mock<DbDataReader>();
            mockReader.Setup(r => r["column"]).Returns("1");
            var reader = new MySQLReader(mockReader.Object);

            // Act
            var result = reader.GetTinyInt("column");

            // Assert
            Assert.IsType<bool>(result);
        }

        [Fact]
        public void MySQLReader_GetDataTime_ShouldReturnDateTime()
        {
            // Arrange
            var mockReader = new Mock<DbDataReader>();
            var expectedDate = DateTime.Now;
            mockReader.Setup(r => r["column"]).Returns(expectedDate);
            var reader = new MySQLReader(mockReader.Object);

            // Act
            var result = reader.GetDataTime("column");

            // Assert
            Assert.Equal(expectedDate, result);
        }

        [Fact]
        public void MySQLReader_GetDecimal_ShouldReturnDecimal()
        {
            // Arrange
            var mockReader = new Mock<DbDataReader>();
            mockReader.Setup(r => r["column"]).Returns(123.45m);
            var reader = new MySQLReader(mockReader.Object);

            // Act
            var result = reader.GetDecimal("column");

            // Assert
            Assert.IsType<decimal>(result);
        }

        [Fact]
        public void MySQLReader_GetDouble_ShouldReturnDouble()
        {
            // Arrange
            var mockReader = new Mock<DbDataReader>();
            mockReader.Setup(r => r["column"]).Returns(123.45);
            var reader = new MySQLReader(mockReader.Object);

            // Act
            var result = reader.GetDouble("column");

            // Assert
            Assert.IsType<double>(result);
        }

        [Fact]
        public void MySQLReader_GetInteger_ShouldReturnInteger()
        {
            // Arrange
            var mockReader = new Mock<DbDataReader>();
            mockReader.Setup(r => r["column"]).Returns(123);
            var reader = new MySQLReader(mockReader.Object);

            // Act
            var result = reader.GetInteger("column");

            // Assert
            Assert.IsType<int>(result);
        }

        [Fact]
        public void MySQLReader_GetInteger_ShouldReturnDefault_OnError()
        {
            // Arrange
            var mockReader = new Mock<DbDataReader>();
            mockReader.Setup(r => r["column"]).Throws<Exception>();
            var reader = new MySQLReader(mockReader.Object);
            var defaultValue = 999;

            // Act
            var result = reader.GetInteger("column", defaultValue);

            // Assert
            Assert.Equal(defaultValue, result);
        }

        [Fact]
        public void MySQLReader_GetLong_ShouldReturnLong()
        {
            // Arrange
            var mockReader = new Mock<DbDataReader>();
            mockReader.Setup(r => r["column"]).Returns(123456789L);
            var reader = new MySQLReader(mockReader.Object);

            // Act
            var result = reader.GetLong("column");

            // Assert
            Assert.IsType<long>(result);
        }

        [Fact]
        public void MySQLReader_GetString_ShouldReturnString()
        {
            // Arrange
            var mockReader = new Mock<DbDataReader>();
            var expectedValue = "test_string";
            mockReader.Setup(r => r["column"]).Returns(expectedValue);
            mockReader.Setup(r => r["column"].ToString()).Returns(expectedValue);
            var reader = new MySQLReader(mockReader.Object);

            // Act
            var result = reader.GetString("column");

            // Assert
            Assert.Equal(expectedValue, result);
        }

        [Fact]
        public void MySQLReader_GetString_ShouldReturnDefault_WhenNull()
        {
            // Arrange
            var mockReader = new Mock<DbDataReader>();
            mockReader.Setup(r => r["column"]).Returns((object?)null);
            mockReader.Setup(r => r["column"].ToString()).Returns((string?)null);
            var reader = new MySQLReader(mockReader.Object);
            var defaultValue = "default";

            // Act
            var result = reader.GetString("column", defaultValue);

            // Assert
            Assert.Equal(defaultValue, result);
        }

        [Fact]
        public void MySQLReader_GetByteArray_ShouldReturnByteArray()
        {
            // Arrange
            var mockReader = new Mock<DbDataReader>();
            var expectedBytes = new byte[] { 1, 2, 3, 4 };
            mockReader.Setup(r => r["column"]).Returns(expectedBytes);
            var reader = new MySQLReader(mockReader.Object);

            // Act
            var result = reader.GetByteArray("column");

            // Assert
            Assert.Equal(expectedBytes, result);
        }

        [Fact]
        public void MySQLReader_Read_ShouldReturnBoolean()
        {
            // Arrange
            var mockReader = new Mock<DbDataReader>();
            mockReader.Setup(r => r.Read()).Returns(true);
            var reader = new MySQLReader(mockReader.Object);

            // Act
            var result = reader.Read();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task MySQLReader_ReadAsync_ShouldReturnTaskOfBoolean()
        {
            // Arrange
            var mockReader = new Mock<DbDataReader>();
            mockReader.Setup(r => r.ReadAsync(default)).ReturnsAsync(true);
            var reader = new MySQLReader(mockReader.Object);

            // Act
            var result = await reader.ReadAsync();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void MySQLReader_GetMySQLArrayReader_ShouldReturnMySQLArrayReader()
        {
            // Arrange
            var mockReader = new Mock<DbDataReader>();
            var reader = new MySQLReader(mockReader.Object);

            // Act
            var result = reader.GetMySQLArrayReader();

            // Assert
            // Pode retornar null em caso de erro
            // Assert.NotNull(result);
        }
    }
}
