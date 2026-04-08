using System;
using System.Collections.Generic;
using Jovemnf.MySQL;
using Jovemnf.MySQL.Builder;
using Xunit;

namespace MysqlTest;

[DbTable("batch_devices")]
public class BatchDevice
{
    [DbField("device_id")]
    public int DeviceId { get; set; }

    [DbField("plate")]
    public string Plate { get; set; } = null!;

    [DbField("status")]
    public string Status { get; set; } = null!;
}

public class InsertBatchQueryBuilderTests
{
    [Fact]
    public void Build_BatchInsert_GeneratesParameterizedSql()
    {
        var builder = new InsertBatchQueryBuilder()
            .Table("positions")
            .Row(new Dictionary<string, object>
            {
                ["tracker_id"] = 1,
                ["status"] = "online"
            })
            .Row(new Dictionary<string, object>
            {
                ["tracker_id"] = 2,
                ["status"] = "offline"
            });

        var (sql, command) = builder.Build();

        Assert.Contains("INSERT INTO `positions` (`tracker_id`, `status`) VALUES", sql);
        Assert.Contains("(@p0, @p1), (@p2, @p3)", sql);
        Assert.Equal(4, command.Parameters.Count);
        Assert.Equal(1, command.Parameters["@p0"].Value);
        Assert.Equal("online", command.Parameters["@p1"].Value);
        Assert.Equal(2, command.Parameters["@p2"].Value);
        Assert.Equal("offline", command.Parameters["@p3"].Value);
    }

    [Fact]
    public void Build_WithDuplicateKeyUpdate_GeneratesUpsertClause()
    {
        var builder = new InsertBatchQueryBuilder()
            .Table("positions")
            .Row(new Dictionary<string, object>
            {
                ["tracker_id"] = 1,
                ["status"] = "online",
                ["speed"] = 80
            })
            .Row(new Dictionary<string, object>
            {
                ["tracker_id"] = 2,
                ["status"] = "offline",
                ["speed"] = 0
            })
            .OnDuplicateKeyUpdate("status", "speed");

        var (sql, _) = builder.Build();

        Assert.Contains("ON DUPLICATE KEY UPDATE", sql);
        Assert.Contains("`status` = VALUES(`status`)", sql);
        Assert.Contains("`speed` = VALUES(`speed`)", sql);
    }

    [Fact]
    public void Build_WithDuplicateKeyUpdateAllExcept_ExcludesSpecifiedFields()
    {
        var builder = new InsertBatchQueryBuilder()
            .Table("positions")
            .Row(new Dictionary<string, object>
            {
                ["tracker_id"] = 1,
                ["status"] = "online",
                ["speed"] = 80
            })
            .OnDuplicateKeyUpdateAllExcept("tracker_id");

        var (sql, _) = builder.Build();

        Assert.DoesNotContain("`tracker_id` = VALUES(`tracker_id`)", sql);
        Assert.Contains("`status` = VALUES(`status`)", sql);
        Assert.Contains("`speed` = VALUES(`speed`)", sql);
    }

    [Fact]
    public void RowsFrom_GenericBuilder_MapsFieldsCorrectly()
    {
        var sql = InsertBatchQueryBuilder.For<BatchDevice>()
            .RowsFrom(new[]
            {
                new BatchDevice { DeviceId = 1, Plate = "ABC1234", Status = "A" },
                new BatchDevice { DeviceId = 2, Plate = "DEF5678", Status = "I" }
            })
            .OnDuplicateKeyUpdate("Status")
            .ToString();

        Assert.Contains("INSERT INTO `batch_devices`", sql);
        Assert.Contains("(`device_id`, `plate`, `status`)", sql);
        Assert.Contains("ON DUPLICATE KEY UPDATE `status` = VALUES(`status`)", sql);
    }

    [Fact]
    public void Rows_WithMapperFunction_MapsArrayToBatchRows()
    {
        var items = new[]
        {
            new { TrackerId = 10, Status = "online" },
            new { TrackerId = 11, Status = "offline" }
        };

        var builder = new InsertBatchQueryBuilder()
            .Table("positions")
            .Rows(items, item => new Dictionary<string, object>
            {
                ["tracker_id"] = item.TrackerId,
                ["status"] = item.Status
            });

        var (sql, command) = builder.Build();

        Assert.Contains("INSERT INTO `positions` (`tracker_id`, `status`) VALUES", sql);
        Assert.Contains("(@p0, @p1), (@p2, @p3)", sql);
        Assert.Equal(4, command.Parameters.Count);
        Assert.Equal(10, command.Parameters["@p0"].Value);
        Assert.Equal("online", command.Parameters["@p1"].Value);
        Assert.Equal(11, command.Parameters["@p2"].Value);
        Assert.Equal("offline", command.Parameters["@p3"].Value);
    }

    [Fact]
    public void Row_ThrowsWhenColumnsDoNotMatch()
    {
        var builder = new InsertBatchQueryBuilder()
            .Table("positions")
            .Row(new Dictionary<string, object>
            {
                ["tracker_id"] = 1,
                ["status"] = "online"
            });

        var ex = Assert.Throws<InvalidOperationException>(() =>
            builder.Row(new Dictionary<string, object>
            {
                ["tracker_id"] = 2
            }));

        Assert.Contains("mesma quantidade de colunas", ex.Message);
    }
}
