using System;
using Jovemnf.MySQL.Builder;
using Xunit;

namespace MysqlTest;

public class DateTimeQueryConversionTests
{
    [Fact]
    public void SelectQueryBuilder_WhereBetweenDateTime_ConvertsParameters()
    {
        var builder = new SelectQueryBuilder()
            .Table("eventos")
            .UseDateTimeTimeZone("-03:00", "UTC")
            .WhereBetweenDateTime(
                "created_at",
                new DateTime(2024, 1, 1, 9, 0, 0),
                new DateTime(2024, 1, 1, 12, 0, 0));

        var (sql, command) = builder.Build();

        Assert.Contains("WHERE `created_at` BETWEEN @p0 AND @p1", sql);
        Assert.Equal(new DateTime(2024, 1, 1, 12, 0, 0), command.Parameters["@p0"].Value);
        Assert.Equal(new DateTime(2024, 1, 1, 15, 0, 0), command.Parameters["@p1"].Value);
    }

    [Fact]
    public void UpdateQueryBuilder_WhereDateTime_ConvertsParameter()
    {
        var builder = new UpdateQueryBuilder()
            .Table("eventos")
            .Set("status", "processado")
            .UseDateTimeTimeZone("-03:00", "UTC")
            .WhereDateTime("processed_at", new DateTime(2024, 1, 1, 8, 30, 0), ">=");

        var (sql, command) = builder.Build();

        Assert.Contains("WHERE `processed_at` >= @p1", sql);
        Assert.Equal(new DateTime(2024, 1, 1, 11, 30, 0), command.Parameters["@p1"].Value);
    }

    [Fact]
    public void DeleteQueryBuilder_OrWhereDateTime_ConvertsParameter()
    {
        var builder = new DeleteQueryBuilder()
            .Table("logs")
            .Where("tipo", "debug")
            .UseDateTimeTimeZone("-03:00", "UTC")
            .OrWhereDateTime("created_at", new DateTime(2024, 2, 1, 20, 0, 0), "<");

        var (sql, command) = builder.Build();

        Assert.Contains("OR `created_at` < @p1", sql);
        Assert.Equal(new DateTime(2024, 2, 1, 23, 0, 0), command.Parameters["@p1"].Value);
    }

    [Fact]
    public void WhereDateTime_WithoutConfiguredTimeZone_Throws()
    {
        var builder = new SelectQueryBuilder().Table("eventos");

        var ex = Assert.Throws<InvalidOperationException>(() =>
            builder.WhereDateTime("created_at", new DateTime(2024, 1, 1, 9, 0, 0)));

        Assert.Contains("UseDateTimeTimeZone", ex.Message);
    }
}
