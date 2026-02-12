using System;
using Jovemnf.MySQL.Builder;
using Xunit;

namespace MysqlTest;

public class DeleteQueryBuilderTests
{
    [Fact]
    public void TestSimpleDelete()
    {
        var builder = new DeleteQueryBuilder()
            .Table("users")
            .Where("id", 1);

        var (sql, command) = builder.Build();

        Assert.Contains("DELETE FROM `users`", sql);
        Assert.Contains("WHERE `id` = @p0", sql);
        Assert.Equal(1, command.Parameters.Count);
    }

    [Fact]
    public void TestDeleteWithQueryOperator()
    {
        var builder = new DeleteQueryBuilder()
            .Table("logs")
            .Where("created_at", "2023-01-01", QueryOperator.LessThan);

        var (sql, command) = builder.Build();

        Assert.Contains("`created_at` < @p0", sql);
    }

    [Fact]
    public void TestDeleteAllProtection_ShouldThrow()
    {
        var builder = new DeleteQueryBuilder()
            .Table("users");

        // Should prevent delete without where
        var ex = Assert.Throws<InvalidOperationException>(() => builder.Build());
        Assert.Equal("Nenhuma condição WHERE definida. Use .All() se realmente deseja deletar todas as linhas.", ex.Message);
    }

    [Fact]
    public void TestDeleteAll_ShouldWork()
    {
        var builder = new DeleteQueryBuilder()
            .Table("users")
            .All();

        var (sql, command) = builder.Build();

        Assert.Contains("DELETE FROM `users`", sql);
        Assert.DoesNotContain("WHERE", sql);
    }

    [Fact]
    public void TestDeleteWithLimit()
    {
        var builder = new DeleteQueryBuilder()
            .Table("logs")
            .Where("status", "expired")
            .Limit(100);

        var (sql, command) = builder.Build();

        Assert.Contains("LIMIT 100", sql);
    }
}
