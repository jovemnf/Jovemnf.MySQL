using Jovemnf.MySQL;
using Jovemnf.MySQL.Builder;
using Jovemnf.MySQL.Geometry;
using Xunit;

namespace MysqlTest;

[DbTable("users")]
public class TestUser
{
    [DbField("user_id")]
    public int Id { get; set; }

    [DbField("full_name")]
    public string Name { get; set; }

    [DbField("user_email")]
    public string Email { get; set; }
}

public class ComprehensiveMappingTests
{
    [Fact]
    public void SelectQueryBuilder_ComprehensiveMapping()
    {
        var sql = SelectQueryBuilder.For<TestUser>()
            .Select("Id", "full_name")
            .Join("orders", "Id", "=", "orders.user_id")
            .Where("Name", "John")
            .OrWhere("user_email", "john@example.com")
            .WhereIn("Id", new[] { 1, 2 })
            .WhereNull("Email")
            .WhereBetween("Id", 10, 20)
            .WhereLike("Name", "J%")
            .OrderBy("Id", "DESC")
            .ToString();

        // Check Table
        Assert.Contains("FROM `users`", sql);

        // Check Select (PascalCase and snake_case mapping)
        Assert.Contains("SELECT `user_id`, `full_name`", sql);

        // Check Join (mapping property Id to user_id)
        Assert.Contains("INNER JOIN `orders` ON `user_id` = `orders`.`user_id`", sql);

        // Check Where (PascalCase)
        Assert.Contains("`full_name` = @p0", sql);

        // Check OrWhere (SnakeCase)
        Assert.Contains("OR `user_email` = @p1", sql);

        // Check WhereIn
        Assert.Contains("`user_id` IN (@p2, @p3)", sql);

        // Check WhereNull
        Assert.Contains("`user_email` IS NULL", sql);

        // Check WhereBetween
        Assert.Contains("`user_id` BETWEEN @p4 AND @p5", sql);

        // Check WhereLike
        Assert.Contains("`full_name` LIKE @p6", sql);

        // Check OrderBy
        Assert.Contains("ORDER BY `user_id` DESC", sql);
    }

    [Fact]
    public void InsertQueryBuilder_ComprehensiveMapping()
    {
        var sql = new InsertQueryBuilder<TestUser>()
            .Value("Id", 1)
            .Value("full_name", "John")
            .ValueAsJson("Email", new { some = "json" })
            .ToString();

        Assert.Contains("INSERT INTO `users`", sql);
        Assert.Contains("(`user_id`, `full_name`, `user_email`)", sql);
    }

    [Fact]
    public void UpdateQueryBuilder_ComprehensiveMapping()
    {
        var sql = new UpdateQueryBuilder<TestUser>()
            .Set("Name", "New John")
            .SetAsJson("Email", "new@example.com")
            .Where("Id", 1)
            .WhereNotNull("Name")
            .ToString();

        Assert.Contains("UPDATE `users`", sql);
        Assert.Contains("SET `full_name` = @p0, `user_email` = @p1", sql);
        Assert.Contains("WHERE `user_id` = @p2 AND `full_name` IS NOT NULL", sql);
    }

    [Fact]
    public void DeleteQueryBuilder_ComprehensiveMapping()
    {
        var sql = new DeleteQueryBuilder<TestUser>()
            .Where("Id", 1)
            .WhereNotIn("Name", new[] { "A", "B" })
            .ToString();

        Assert.Contains("DELETE FROM `users`", sql);
        Assert.Contains("WHERE `user_id` = @p0 AND `full_name` NOT IN (@p1, @p2)", sql);
    }

    [Fact]
    public void Join_AvoidsMapping_WhenDotIsPresent()
    {
        var sql = SelectQueryBuilder.For<TestUser>()
            .Join("other", "other.user_id", "=", "Id")
            .ToString();

        // first part "other.user_id" should remain as is
        // second part "Id" should be resolved to "user_id"
        Assert.Contains("ON `other`.`user_id` = `user_id`", sql);
    }
}
