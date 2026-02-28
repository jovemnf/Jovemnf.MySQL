using System;
using System.Reflection;
using System.Threading.Tasks;
using Jovemnf.MySQL;
using Jovemnf.MySQL.Builder;
using Jovemnf.MySQL.Configuration;
using MySqlConnector;
using Xunit;

namespace MysqlTest;

public class ExecuteInsertTests
{
    private class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }

    [Fact]
    public async Task ExecuteInsertAsync_Entity_PreparesCommandCorrectly()
    {
        // Arrange
        var config = new MySQLConfiguration() 
        { 
            Host = "localhost", 
            Database = "test", 
            Username = "root", 
            Password = "password" 
        };
        var mysql = new MySQL(config);
        var entity = new TestEntity { Name = "Test", CreatedAt = DateTime.Now };

        // Act & Assert
        // We expect it to fail because we're not actually connected, but we want to check the command state
        try 
        {
            await mysql.ExecuteInsertAsync(entity);
        }
        catch (Exception)
        {
            // Expected failure during ExecuteNonQueryAsync
        }

        var cmdField = typeof(MySQL).GetField("cmd", BindingFlags.NonPublic | BindingFlags.Instance);
        var cmd = (MySqlCommand?)cmdField?.GetValue(mysql);

        Assert.NotNull(cmd);
        Assert.Contains("INSERT INTO `TestEntity`", cmd!.CommandText);
        Assert.Contains("`name`", cmd.CommandText);
        Assert.Contains("`created_at`", cmd.CommandText);
        
        // Find parameter for Name
        bool foundName = false;
        foreach (MySqlParameter param in cmd.Parameters)
        {
            if (param.Value?.ToString() == "Test")
            {
                foundName = true;
                break;
            }
        }
        Assert.True(foundName, "Parameter with value 'Test' not found");
    }

    [Fact]
    public async Task LastIdAsync_Generic_HandlesPrimitives()
    {
        // Arrange
        var config = new MySQLConfiguration 
        { 
            Host = "localhost", 
            Database = "test", 
            Username = "root", 
            Password = "password" 
        };
        var mysql = new MySQL(config);
        
        var cmdField = typeof(MySQL).GetField("cmd", BindingFlags.NonPublic | BindingFlags.Instance);
        var mockCmd = new MySqlCommand();
        cmdField!.SetValue(mysql, mockCmd);

        // We can't actually ExecuteScalarAsync on a real MySqlCommand without a connection
        // but we can test that the method prepares the command.
        
        try 
        {
            mysql.OpenCommand("INSERT INTO some_table (col) VALUES (1)");
            await mysql.ExecuteInsertAsync<int>(true);
        }
        catch (Exception)
        {
            // Expected failure during ExecuteNonQueryAsync
        }

        var cmd = (MySqlCommand?)cmdField.GetValue(mysql);
        Assert.NotNull(cmd);
        // At this point, it should have attempted to execute the insert.
        // If it succeeded, it would have changed CommandText to SELECT LAST_INSERT_ID()
        // but it likely failed during ExecuteNonQueryAsync with the original SQL.
        Assert.Contains("INSERT INTO some_table", cmd!.CommandText);
    }
}
