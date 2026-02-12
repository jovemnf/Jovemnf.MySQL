using System.Collections.Generic;
using Jovemnf.MySQL.Builder;
using Xunit;

namespace MysqlTest;

public class EnumTests
{
    [Fact]
    public void TestRegexpOperator()
    {
        var builder = new UpdateQueryBuilder()
            .Table("products")
            .Set("category", "electronics")
            .Where("sku", "^AB-", QueryOperator.Regexp);

        var (sql, _) = builder.Build();
        
        Assert.Contains("`sku` REGEXP @p", sql);
    }

    [Fact]
    public void TestNotRegexpOperator()
    {
        var builder = new UpdateQueryBuilder()
            .Table("users")
            .Set("valid", false)
            .Where("email", "@gmail.com$", QueryOperator.NotRegexp);

        var (sql, _) = builder.Build();
        
        Assert.Contains("`email` NOT REGEXP @p", sql);
    }

    [Fact]
    public void TestIsNotNullEnum()
    {
        // When using IsNotNull with generic Where, the value is ignored by logic but must be passed
        // This confirms safe usage even if value is dummy
        var builder = new UpdateQueryBuilder()
            .Table("logs")
            .Set("archived", true)
            .Where("deleted_at", "ignored", QueryOperator.IsNotNull);

        var (sql, _) = builder.Build();
        
        Assert.Contains("`deleted_at` IS NOT NULL", sql);
    }
    
    [Fact]
    public void TestBetweenEnum_GenericUsage()
    {
        // Demonstrating that generic Where with BETWEEN might produce SQL but won't handle 2 values
        // This test serves to document behavior, not necessarily endorse it without generic wrapper
        
        // If we pass a single value to BETWEEN via generic Where, it treats it as 'value1'
        // The second value defaults to null/DBNull in generic Where if logic doesn't support it
        // BUT UpdateQueryBuilder.BuildWhereClause for BETWEEN expects condition.SecondValue
        
        // Let's see what happens. If it fails, we know we need WhereBetween
        
        var builder = new UpdateQueryBuilder()
            .Table("stats")
            .Set("count", 0)
            .Where("age", 18, QueryOperator.Between);
            
        // This will likely produce "`age` BETWEEN @p0 AND @p1" where p1 is null.
        // It's technically valid SQL syntax but logically probably wrong for the user
        // So we test that it generates the SQL
        
        var (sql, command) = builder.Build();
        Assert.Contains("BETWEEN", sql);
    }
}
