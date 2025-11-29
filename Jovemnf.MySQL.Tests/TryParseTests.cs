using Xunit;
using System;
using System.Reflection;

namespace Jovemnf.MySQL.Tests
{
    public class TryParseTests
    {
        // Como TryParse é internal, precisamos usar reflection para testá-lo
        private Type GetTryParseType()
        {
            var assembly = typeof(Jovemnf.MySQL.MySQL).Assembly;
            return assembly.GetType("Jovemnf.MySQL.TryParse")!;
        }

        [Fact]
        public void TryParse_ToBoolean_ShouldReturnTrue_ForOne()
        {
            // Arrange
            var type = GetTryParseType();
            var method = type.GetMethod("ToBoolean", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(method);

            // Act
            var result = method!.Invoke(null, new object[] { 1 });

            // Assert
            Assert.True((bool)result!);
        }

        [Fact]
        public void TryParse_ToBoolean_ShouldReturnFalse_ForZero()
        {
            // Arrange
            var type = GetTryParseType();
            var method = type.GetMethod("ToBoolean", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(method);

            // Act
            var result = method!.Invoke(null, new object[] { 0 });

            // Assert
            Assert.False((bool)result!);
        }

        [Fact]
        public void TryParse_ToBoolean_ShouldReturnFalse_OnError()
        {
            // Arrange
            var type = GetTryParseType();
            var method = type.GetMethod("ToBoolean", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(method);

            // Act
            var result = method!.Invoke(null, new object[] { "invalid" });

            // Assert
            Assert.False((bool)result!);
        }

        [Fact]
        public void TryParse_ToDecimal_ShouldReturnDecimal()
        {
            // Arrange
            var type = GetTryParseType();
            var method = type.GetMethod("ToDecimal", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(method);

            // Act
            var result = method!.Invoke(null, new object[] { 123.45m });

            // Assert
            Assert.Equal(123.45m, result);
        }

        [Fact]
        public void TryParse_ToDecimal_ShouldReturnZero_OnError()
        {
            // Arrange
            var type = GetTryParseType();
            var method = type.GetMethod("ToDecimal", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(method);

            // Act
            var result = method!.Invoke(null, new object[] { "invalid" });

            // Assert
            Assert.Equal(0m, result);
        }

        [Fact]
        public void TryParse_ToDouble_ShouldReturnDouble()
        {
            // Arrange
            var type = GetTryParseType();
            var method = type.GetMethod("ToDouble", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(method);

            // Act
            var result = method!.Invoke(null, new object[] { 123.45 });

            // Assert
            Assert.Equal(123.45, result);
        }

        [Fact]
        public void TryParse_ToDouble_ShouldReturnZero_OnError()
        {
            // Arrange
            var type = GetTryParseType();
            var method = type.GetMethod("ToDouble", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(method);

            // Act
            var result = method!.Invoke(null, new object[] { "invalid" });

            // Assert
            Assert.Equal(0.00, result);
        }

        [Fact]
        public void TryParse_ToLong_ShouldReturnLong()
        {
            // Arrange
            var type = GetTryParseType();
            var method = type.GetMethod("ToLong", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(method);

            // Act
            var result = method!.Invoke(null, new object[] { 123456789L });

            // Assert
            Assert.Equal(123456789L, result);
        }

        [Fact]
        public void TryParse_ToLong_ShouldReturnZero_OnError()
        {
            // Arrange
            var type = GetTryParseType();
            var method = type.GetMethod("ToLong", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(method);

            // Act
            var result = method!.Invoke(null, new object[] { "invalid" });

            // Assert
            Assert.Equal(0L, result);
        }

        [Fact]
        public void TryParse_ToInt32_ShouldReturnInt32()
        {
            // Arrange
            var type = GetTryParseType();
            var method = type.GetMethod("ToInt32", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(method);

            // Act
            var result = method!.Invoke(null, new object[] { 123 });

            // Assert
            Assert.Equal(123, result);
        }

        [Fact]
        public void TryParse_ToInt32_ShouldReturnZero_OnError()
        {
            // Arrange
            var type = GetTryParseType();
            var method = type.GetMethod("ToInt32", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(method);

            // Act
            var result = method!.Invoke(null, new object[] { "invalid" });

            // Assert
            Assert.Equal(0, result);
        }
    }
}
