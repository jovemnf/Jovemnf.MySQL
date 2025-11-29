using Xunit;
using Jovemnf.MySQL;
using System;

namespace Jovemnf.MySQL.Tests
{
    public class ExceptionTests
    {
        [Fact]
        public void MySQLConnectException_DefaultConstructor_ShouldCreateException()
        {
            // Act
            var exception = new MySQLConnectException();

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<MySQLConnectException>(exception);
        }

        [Fact]
        public void MySQLConnectException_WithMessage_ShouldSetMessage()
        {
            // Arrange
            var message = "Test connection error";

            // Act
            var exception = new MySQLConnectException(message);

            // Assert
            Assert.Equal(message, exception.Message);
        }

        [Fact]
        public void MySQLConnectException_WithFormat_ShouldFormatMessage()
        {
            // Arrange
            var format = "Connection failed: {0}";
            var arg = "timeout";

            // Act
            var exception = new MySQLConnectException(format, arg);

            // Assert
            Assert.Contains("timeout", exception.Message);
        }

        [Fact]
        public void MySQLConnectException_WithInnerException_ShouldSetInnerException()
        {
            // Arrange
            var innerException = new Exception("Inner error");
            var message = "Connection error";

            // Act
            var exception = new MySQLConnectException(message, innerException);

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.Equal(innerException, exception.InnerException);
        }

        [Fact]
        public void MySQLCloseException_DefaultConstructor_ShouldCreateException()
        {
            // Act
            var exception = new MySQLCloseException();

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<MySQLCloseException>(exception);
        }

        [Fact]
        public void MySQLCloseException_WithMessage_ShouldSetMessage()
        {
            // Arrange
            var message = "Test close error";

            // Act
            var exception = new MySQLCloseException(message);

            // Assert
            Assert.Equal(message, exception.Message);
        }

        [Fact]
        public void MySQLCloseException_WithFormat_ShouldFormatMessage()
        {
            // Arrange
            var format = "Close failed: {0}";
            var arg = "connection lost";

            // Act
            var exception = new MySQLCloseException(format, arg);

            // Assert
            Assert.Contains("connection lost", exception.Message);
        }

        [Fact]
        public void MySQLCloseException_WithInnerException_ShouldSetInnerException()
        {
            // Arrange
            var innerException = new Exception("Inner error");
            var message = "Close error";

            // Act
            var exception = new MySQLCloseException(message, innerException);

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.Equal(innerException, exception.InnerException);
        }
    }
}
