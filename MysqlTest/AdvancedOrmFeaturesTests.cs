using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jovemnf.MySQL;
using Jovemnf.MySQL.Builder;
using Microsoft.Extensions.Logging.Abstractions;
using MysqlTest.Fakes;
using MySqlConnector;
using Xunit;

namespace MysqlTest;

[Collection(MutationProtectionTestCollection.Name)]
public class AdvancedOrmFeaturesTests
{
    [Fact]
    public void Insert_Build_WithUpsertClause_GeneratesExpectedSql()
    {
        var builder = new InsertQueryBuilder()
            .Table("rastreadores")
            .Value("tracker_id", 10)
            .Value("status", "online")
            .OnDuplicateKeyUpdate("status");

        var (sql, _) = builder.Build();

        Assert.Contains("INSERT INTO `rastreadores`", sql);
        Assert.Contains("ON DUPLICATE KEY UPDATE `status` = VALUES(`status`)", sql);
    }

    [Fact]
    public void Insert_Build_WithUpsertAllExcept_GeneratesExpectedSql()
    {
        var builder = new InsertQueryBuilder()
            .Table("rastreadores")
            .Value("tracker_id", 10)
            .Value("status", "online")
            .Value("placa", "ABC1234")
            .OnDuplicateKeyUpdateAllExcept("tracker_id");

        var (sql, _) = builder.Build();

        Assert.DoesNotContain("`tracker_id` = VALUES(`tracker_id`)", sql);
        Assert.Contains("`status` = VALUES(`status`)", sql);
        Assert.Contains("`placa` = VALUES(`placa`)", sql);
    }

    [Fact]
    public void Select_Build_WithWhereExistsAndSubqueryIn_GeneratesExpectedSql()
    {
        var existsSubquery = new SelectQueryBuilder()
            .SelectRaw("1")
            .From("rastreamento_eventos re")
            .WhereRaw("re.veiculo_id = v.id")
            .Where("ativo", true);

        var inSubquery = new SelectQueryBuilder()
            .Select("veiculo_id")
            .From("rastreamento_eventos")
            .Where("status", "online");

        var builder = new SelectQueryBuilder()
            .Select("v.id", "v.placa")
            .From("veiculos v")
            .WhereExists(existsSubquery)
            .WhereIn("v.id", inSubquery);

        var (sql, command) = builder.Build();

        Assert.Contains("EXISTS (SELECT 1 FROM `rastreamento_eventos re` WHERE re.veiculo_id = v.id AND `ativo` = @p0)", sql);
        Assert.Contains("AND `v`.`id` IN (SELECT `veiculo_id` FROM `rastreamento_eventos` WHERE `status` = @p1)", sql);
        Assert.Equal(2, command.Parameters.Count);
    }

    [Fact]
    public void Select_BuildCount_WithGroupedQuery_WrapsSubquery()
    {
        var builder = new SelectQueryBuilder()
            .Select("status")
            .SelectRaw("COUNT(*) AS total")
            .From("rastreamento_eventos")
            .GroupBy("status")
            .HavingRaw("COUNT(*) > {0}", 1);

        var (sql, command) = builder.BuildCount();

        Assert.StartsWith("SELECT COUNT(*) FROM (SELECT", sql);
        Assert.Contains("FROM `rastreamento_eventos`", sql);
        Assert.Contains("GROUP BY `status`", sql);
        Assert.Contains("HAVING COUNT(*) >", sql);
        Assert.EndsWith("AS `count_query`", sql);
        Assert.Single(command.Parameters);
    }

    [Fact]
    public void Update_All_WithGlobalConfirmationRequired_ThrowsWithoutConfirmation()
    {
        var previous = MySQL.DefaultOptions.MutationProtection.RequireConfirmationForAllOperations;
        MySQL.DefaultOptions.MutationProtection.RequireConfirmationForAllOperations = true;

        try
        {
            var builder = new UpdateQueryBuilder()
                .Table("veiculos")
                .Set("ativo", false)
                .All();

            var ex = Assert.Throws<InvalidOperationException>(() => builder.Build());
            Assert.Contains("ConfirmMassOperation", ex.Message);
        }
        finally
        {
            MySQL.DefaultOptions.MutationProtection.RequireConfirmationForAllOperations = previous;
        }
    }

    [Fact]
    public void Delete_All_WithGlobalLimitRequired_ThrowsWithoutLimit()
    {
        var previous = MySQL.DefaultOptions.MutationProtection.RequireLimitForDeleteAllOperations;
        MySQL.DefaultOptions.MutationProtection.RequireLimitForDeleteAllOperations = true;

        try
        {
            var builder = new DeleteQueryBuilder()
                .Table("eventos")
                .All();

            var ex = Assert.Throws<InvalidOperationException>(() => builder.Build());
            Assert.Contains("LIMIT", ex.Message);
        }
        finally
        {
            MySQL.DefaultOptions.MutationProtection.RequireLimitForDeleteAllOperations = previous;
        }
    }

    [Fact]
    public void Update_All_WithConfirmationAndLimit_GeneratesExpectedSql()
    {
        var previousConfirmation = MySQL.DefaultOptions.MutationProtection.RequireConfirmationForAllOperations;
        var previousLimit = MySQL.DefaultOptions.MutationProtection.RequireLimitForUpdateAllOperations;
        MySQL.DefaultOptions.MutationProtection.RequireConfirmationForAllOperations = true;
        MySQL.DefaultOptions.MutationProtection.RequireLimitForUpdateAllOperations = true;

        try
        {
            var builder = new UpdateQueryBuilder()
                .Table("veiculos")
                .Set("ativo", false)
                .All()
                .ConfirmMassOperation()
                .Limit(50);

            var (sql, _) = builder.Build();
            Assert.Equal("UPDATE `veiculos` SET `ativo` = @p0 LIMIT 50", sql);
        }
        finally
        {
            MySQL.DefaultOptions.MutationProtection.RequireConfirmationForAllOperations = previousConfirmation;
            MySQL.DefaultOptions.MutationProtection.RequireLimitForUpdateAllOperations = previousLimit;
        }
    }

    [Fact]
    public async Task Reader_ToModelListAsync_WithCancellationToken_MapsRows()
    {
        var data = new List<Dictionary<string, object>>
        {
            new()
            {
                ["id"] = 1,
                ["nome"] = "Rastreador 1",
                ["ativo"] = true,
                ["salario"] = 0d,
                ["data_nascimento"] = new DateTime(2024, 1, 1)
            }
        };

        await using var reader = new MySQLReader(new FakeDataReader(data));
        using var cts = new CancellationTokenSource();
        var models = await reader.ToModelListAsync<TestModel>(cts.Token);

        var model = Assert.Single(models);
        Assert.Equal(1, model.Id);
        Assert.Equal("Rastreador 1", model.Nome);
    }

    [Fact]
    public void Reader_ToModel_UsesRegisteredAndAttributedConverters()
    {
        MySQLValueConverterRegistry.Register<TrackerStatus>(new TrackerStatusConverter());

        var data = new List<Dictionary<string, object>>
        {
            new()
            {
                ["status"] = "online",
                ["placa"] = "abc1234"
            }
        };

        using var reader = new MySQLReader(new FakeDataReader(data));
        reader.Read();

        var model = reader.ToModel<VehicleWithConverters>();

        Assert.Equal(TrackerStatus.Online, model.Status);
        Assert.Equal("ABC1234", model.Placa);
    }

    [Fact]
    public async Task Reader_ToMultiMapListAsync_MapsJoinedRows()
    {
        var data = new List<Dictionary<string, object>>
        {
            new()
            {
                ["user_id"] = 1,
                ["user_name"] = "Joao",
                ["tracker_id"] = 77,
                ["tracker_code"] = "TRK-77"
            }
        };

        await using var reader = new MySQLReader(new FakeDataReader(data));
        var result = await reader.ToMultiMapListAsync<JoinUser, JoinTracker, UserTrackerView>(
            (user, tracker) => new UserTrackerView(user.Name, tracker.Code),
            splitOn: "tracker_id");

        var item = Assert.Single(result);
        Assert.Equal("Joao", item.UserName);
        Assert.Equal("TRK-77", item.TrackerCode);
    }

    [Fact]
    public void MySQL_BuildDebugSql_FormatsParameters()
    {
        using var command = new MySqlCommand("SELECT * FROM logs WHERE tracker_id = @p0 AND ativo = @p1 AND placa = @p2");
        command.Parameters.AddWithValue("@p0", 10);
        command.Parameters.AddWithValue("@p1", true);
        command.Parameters.AddWithValue("@p2", "ABC1234");

        var sql = MySQL.BuildDebugSql(command);

        Assert.Equal("SELECT * FROM logs WHERE tracker_id = 10 AND ativo = 1 AND placa = 'ABC1234'", sql);
    }

    [Fact]
    public void PageRequest_AndPagedResult_ExposeExpectedMetadata()
    {
        var request = new PageRequest(2, 25);
        var result = new PagedResult<int>
        {
            Items = new[] { 1, 2, 3 },
            TotalItems = 60,
            Page = request.Page,
            PageSize = request.PageSize
        };

        Assert.Equal(25, request.Offset);
        Assert.Equal(3, result.TotalPages);
        Assert.True(result.HasNextPage);
        Assert.True(result.HasPreviousPage);
    }

    [Fact]
    public void MySQL_OptionsConstructor_ConfiguresLogger()
    {
        var config = new Jovemnf.MySQL.Configuration.MySQLConfiguration
        {
            Host = "localhost",
            Database = "test",
            Username = "root",
            Password = "password"
        };

        var options = new MySQLOptions { Logger = NullLogger.Instance };
        using var mysql = new MySQL(config, options);

        Assert.Same(options, mysql.Options);
    }
}

public enum TrackerStatus
{
    Offline = 0,
    Online = 1
}

public sealed class TrackerStatusConverter : IMySQLValueConverter
{
    public object ConvertFromDb(object value, Type targetType)
    {
        return string.Equals(value?.ToString(), "online", StringComparison.OrdinalIgnoreCase)
            ? TrackerStatus.Online
            : TrackerStatus.Offline;
    }
}

public sealed class UpperCaseStringConverter : IMySQLValueConverter
{
    public object ConvertFromDb(object value, Type targetType)
    {
        return value?.ToString()?.ToUpperInvariant() ?? string.Empty;
    }
}

public sealed class VehicleWithConverters
{
    public TrackerStatus Status { get; set; }

    [DbConverter(typeof(UpperCaseStringConverter))]
    public string Placa { get; set; } = null!;
}

public sealed class JoinUser
{
    [DbField("user_id")]
    public int Id { get; set; }

    [DbField("user_name")]
    public string Name { get; set; } = null!;
}

public sealed class JoinTracker
{
    [DbField("tracker_id")]
    public int Id { get; set; }

    [DbField("tracker_code")]
    public string Code { get; set; } = null!;
}

public sealed record UserTrackerView(string UserName, string TrackerCode);
