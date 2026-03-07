using System;
using Jovemnf.MySQL;
using Jovemnf.MySQL.Configuration;
using Xunit;

namespace MysqlTest;

public class SessionTimeZoneTests
{
    [Fact]
    public void MySQLConfiguration_PreservesSessionTimeZoneOnConnection()
    {
        var config = new MySQLConfiguration
        {
            Host = "localhost",
            Database = "test",
            Username = "root",
            Password = "password",
            SessionTimeZone = "America/Sao_Paulo"
        };

        var mysql = new MySQL(config);

        Assert.Equal("America/Sao_Paulo", mysql.SessionTimeZone);
    }

    [Fact]
    public void MySQL_StringConnectionConstructor_AllowsSessionTimeZone()
    {
        var mysql = new MySQL("Server=localhost;Database=test;User ID=root;Password=password;", "-03:00");

        Assert.Equal("-03:00", mysql.SessionTimeZone);
    }

    [Fact]
    public void MySQL_Init_PersistsSessionTimeZoneForDefaultConstructor()
    {
        MySQL.Init(new MySQLConfiguration
        {
            Host = "localhost",
            Database = "test",
            Username = "root",
            Password = "password",
            SessionTimeZone = "UTC"
        });

        var mysql = new MySQL();

        Assert.Equal("UTC", mysql.SessionTimeZone);
    }

    [Theory]
    [InlineData("UTC", "SET time_zone = 'UTC'")]
    [InlineData("America/Sao_Paulo", "SET time_zone = 'America/Sao_Paulo'")]
    [InlineData("+00:00", "SET time_zone = '+00:00'")]
    [InlineData(" SYSTEM ", "SET time_zone = 'SYSTEM'")]
    public void BuildSetTimeZoneCommandText_ReturnsExpectedSql(string input, string expected)
    {
        var sql = MySqlSessionTimeZone.BuildSetTimeZoneCommandText(input);

        Assert.Equal(expected, sql);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void BuildSetTimeZoneCommandText_WithEmptyValue_ReturnsEmptyString(string input)
    {
        var sql = MySqlSessionTimeZone.BuildSetTimeZoneCommandText(input);

        Assert.Equal(string.Empty, sql);
    }

    [Theory]
    [InlineData("America/Sao_Paulo'; DROP TABLE users; --")]
    [InlineData("UTC;SET")]
    [InlineData("UTC -- comment")]
    public void Normalize_WithInvalidValue_ThrowsArgumentException(string input)
    {
        Assert.Throws<ArgumentException>(() => MySqlSessionTimeZone.Normalize(input));
    }
}
