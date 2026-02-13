using Jovemnf.MySQL;
using Jovemnf.MySQL.Builder;
using Xunit;

namespace MysqlTest;

[DbTable("custom_table")]
public class TestModelWithAttribute
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public class TestModelWithoutAttribute
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public class DbTableTests
{
    [Fact]
    public void SelectQueryBuilder_ForT_WithAttribute_SetsCorrectTable()
    {
        var sql = SelectQueryBuilder.For<TestModelWithAttribute>().ToString();
        Assert.Contains("FROM `custom_table`", sql);
    }

    [Fact]
    public void SelectQueryBuilder_ForT_WithoutAttribute_SetsClassNameAsTable()
    {
        var sql = SelectQueryBuilder.For<TestModelWithoutAttribute>().ToString();
        Assert.Contains("FROM `TestModelWithoutAttribute`", sql);
    }

    [Fact]
    public void InsertQueryBuilder_Generic_WithAttribute_SetsCorrectTable()
    {
        var sql = new InsertQueryBuilder<TestModelWithAttribute>()
            .Value("Name", "Test")
            .ToString();
        Assert.Contains("INSERT INTO `custom_table`", sql);
    }

    [Fact]
    public void UpdateQueryBuilder_Generic_WithAttribute_SetsCorrectTable()
    {
        var sql = new UpdateQueryBuilder<TestModelWithAttribute>()
            .Set("Name", "NewName")
            .Where("Id", 1)
            .ToString();
        Assert.Contains("UPDATE `custom_table`", sql);
    }

    [Fact]
    public void DeleteQueryBuilder_Generic_WithAttribute_SetsCorrectTable()
    {
        var sql = new DeleteQueryBuilder<TestModelWithAttribute>()
            .Where("Id", 1)
            .ToString();
        Assert.Contains("DELETE FROM `custom_table`", sql);
    }

    [Fact]
    public void ExplicitTable_OverridesAttribute()
    {
        var sql = SelectQueryBuilder.For<TestModelWithAttribute>()
            .Table("explicit_table")
            .ToString();
        Assert.Contains("FROM `explicit_table`", sql);
    }

    [Fact]
    public void SelectQueryBuilder_Generic_MapsPropertyToColumn()
    {
        var sql = SelectQueryBuilder.For<TestModelWithDbField>()
            .Where("PropertyName", "value")
            .ToString();
        Assert.Contains("`real_column` = @p0", sql);
    }

    [Fact]
    public void InsertQueryBuilder_Generic_MapsPropertyToColumn()
    {
        var sql = new InsertQueryBuilder<TestModelWithDbField>()
            .Value("PropertyName", "value")
            .ToString();
        Assert.Contains("(`real_column`)", sql);
    }

    [Fact]
    public void UpdateQueryBuilder_Generic_MapsPropertyToColumn()
    {
        var sql = new UpdateQueryBuilder<TestModelWithDbField>()
            .Set("PropertyName", "value")
            .Where("PropertyName", "value")
            .ToString();
        Assert.Contains("SET `real_column` = @p0", sql);
        Assert.Contains("WHERE `real_column` = @p1", sql);
    }

    [Fact]
    public void SelectQueryBuilder_Generic_MapsSnakeCasePropertyToColumn()
    {
        var sql = SelectQueryBuilder.For<TestModelWithDbField>()
            .Where("property_name", "value")
            .ToString();
        Assert.Contains("`real_column` = @p0", sql);
    }

    [Fact]
    public void SelectQueryBuilder_Generic_MapsSelectAndOrderBy()
    {
        var sql = SelectQueryBuilder.For<TestModelWithDbField>()
            .Select("PropertyName")
            .OrderBy("PropertyName")
            .ToString();
        Assert.Contains("SELECT `real_column`", sql);
        Assert.Contains("ORDER BY `real_column` ASC", sql);
    }
}

[DbTable("mapped_table")]
public class TestModelWithDbField
{
    [DbField("real_column")]
    public string PropertyName { get; set; }
}
