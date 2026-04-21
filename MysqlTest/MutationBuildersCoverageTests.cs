using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Jovemnf.MySQL;
using Jovemnf.MySQL.Builder;
using Jovemnf.MySQL.Configuration;
using Xunit;

namespace MysqlTest;

public class MutationBuildersCoverageTests
{
    private static MySQL CreateConnection() => new MySQL(new MySQLConfiguration
    {
        Host = "localhost",
        Database = "test",
        Username = "root",
        Password = "password"
    });

    private sealed class InsertSource
    {
        public int VehicleId { get; set; }
        public string? Plate { get; set; }

        [IgnoreToDictionary]
        public string? InternalNote { get; set; }
    }

    [DbTable("batch_logs")]
    private sealed class BatchLog
    {
        [DbField("tracker_id")]
        public int TrackerId { get; set; }

        public string? Status { get; set; }

        [IgnoreToDictionary]
        public string? IgnoredField { get; set; }
    }

    [Fact]
    public async Task Insert_ExecuteAsync_ThrowsWhenConnectionNull()
    {
        var builder = new InsertQueryBuilder()
            .Table("veiculos")
            .Value("placa", "ABC1234");

        var ex = await Assert.ThrowsAsync<ArgumentNullException>(() => builder.ExecuteAsync(null!));

        Assert.Equal("connection", ex.ParamName);
    }

    [Fact]
    public async Task Insert_ExecuteAsync_ThrowsWhenTableNull()
    {
        var builder = new InsertQueryBuilder()
            .Value("placa", "ABC1234");
        await using var conn = CreateConnection();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => builder.ExecuteAsync(conn));

        Assert.Equal("Tabela não especificada", ex.Message);
    }

    [Fact]
    public async Task Insert_ExecuteAsync_ThrowsWhenNoFields()
    {
        var builder = new InsertQueryBuilder()
            .Table("veiculos");
        await using var conn = CreateConnection();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => builder.ExecuteAsync(conn));

        Assert.Equal("Nenhum campo para inserir", ex.Message);
    }

    [Fact]
    public void Insert_ExecuteAsync_ReturnsTask()
    {
        var builder = new InsertQueryBuilder()
            .Table("veiculos")
            .Value("placa", "ABC1234");
        using var conn = CreateConnection();

        var task = builder.ExecuteAsync(conn);

        Assert.NotNull(task);
        Assert.IsAssignableFrom<Task>(task);
    }

    [Fact]
    public void Insert_BuildSelectById_ThrowsWhenTableMissing()
    {
        var builder = new InsertQueryBuilder()
            .Value("placa", "ABC1234");

        var ex = Assert.Throws<InvalidOperationException>(() => builder.BuildSelectById(10));

        Assert.Equal("Tabela não especificada", ex.Message);
    }

    [Fact]
    public void Insert_ValuesFrom_ThrowsWhenEntityNull()
    {
        var builder = new InsertQueryBuilder<InsertSource>();

        var ex = Assert.Throws<ArgumentNullException>(() => builder.ValuesFrom(null!));

        Assert.Equal("entity", ex.ParamName);
    }

    [Fact]
    public void Insert_ValuesFrom_SkipsNullsAndIgnoredMembers()
    {
        var builder = new InsertQueryBuilder<InsertSource>()
            .ValuesFrom(new InsertSource
            {
                VehicleId = 15,
                Plate = null,
                InternalNote = "nao deve ir"
            });

        var (sql, command) = builder.Build();

        Assert.Contains("INSERT INTO `InsertSource` (`vehicle_id`)", sql);
        Assert.DoesNotContain("`plate`", sql);
        Assert.DoesNotContain("internal_note", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Single(command.Parameters);
        Assert.Equal(15, command.Parameters["@p0"].Value);
    }

    [Fact]
    public async Task Update_ExecuteAsync_ThrowsWhenConnectionNull()
    {
        var builder = new UpdateQueryBuilder()
            .Table("veiculos")
            .Set("status", "ativo")
            .Where("id", 1);

        var ex = await Assert.ThrowsAsync<ArgumentNullException>(() => builder.ExecuteAsync(null!));

        Assert.Equal("connection", ex.ParamName);
    }

    [Fact]
    public async Task Update_ExecuteAsync_ThrowsWhenTableNull()
    {
        var builder = new UpdateQueryBuilder()
            .Set("status", "ativo")
            .Where("id", 1);
        await using var conn = CreateConnection();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => builder.ExecuteAsync(conn));

        Assert.Equal("Tabela não especificada", ex.Message);
    }

    [Fact]
    public async Task Update_ExecuteAsync_ThrowsWhenNoFields()
    {
        var builder = new UpdateQueryBuilder()
            .Table("veiculos")
            .Where("id", 1);
        await using var conn = CreateConnection();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => builder.ExecuteAsync(conn));

        Assert.Equal("Nenhum campo para atualizar", ex.Message);
    }

    [Fact]
    public void Update_ExecuteAsync_ReturnsTaskOfInt()
    {
        var builder = new UpdateQueryBuilder()
            .Table("veiculos")
            .Set("status", "ativo")
            .Where("id", 1);
        using var conn = CreateConnection();

        var task = builder.ExecuteAsync(conn);

        Assert.NotNull(task);
        Assert.IsAssignableFrom<Task<int>>(task);
    }

    [Fact]
    public void Update_SetDictionary_GeneratesExpectedSql()
    {
        var builder = new UpdateQueryBuilder()
            .Table("veiculos")
            .Set(new Dictionary<string, object>
            {
                ["status"] = "ativo",
                ["placa"] = "ABC1234"
            })
            .Where("id", 1);

        var (sql, command) = builder.Build();

        Assert.Contains("SET `status` = @p0, `placa` = @p1", sql);
        Assert.Contains("WHERE `id` = @p2", sql);
        Assert.Equal(3, command.Parameters.Count);
    }

    [Fact]
    public void Update_OrWhere_StringOperator_GeneratesExpectedSql()
    {
        var builder = new UpdateQueryBuilder()
            .Table("telemetria")
            .Set("status", "alerta")
            .Where("idade", 18, QueryOperator.GreaterThanOrEqual)
            .OrWhere("idade", 60, "<");

        var (sql, command) = builder.Build();

        Assert.Contains("WHERE `idade` >= @p1 OR `idade` < @p2", sql);
        Assert.Equal(3, command.Parameters.Count);
    }

    [Fact]
    public void Update_BuildSelect_ThrowsWhenTableMissing()
    {
        var builder = new UpdateQueryBuilder()
            .Set("status", "ativo")
            .Where("id", 1);

        var ex = Assert.Throws<InvalidOperationException>(() => builder.BuildSelect());

        Assert.Equal("Tabela não especificada", ex.Message);
    }

    [Fact]
    public async Task InsertBatch_ExecuteAsync_ThrowsWhenConnectionNull()
    {
        var builder = new InsertBatchQueryBuilder()
            .Table("posicoes")
            .Row(new Dictionary<string, object>
            {
                ["tracker_id"] = 1,
                ["status"] = "online"
            });

        var ex = await Assert.ThrowsAsync<ArgumentNullException>(() => builder.ExecuteAsync(null!));

        Assert.Equal("connection", ex.ParamName);
    }

    [Fact]
    public async Task InsertBatch_ExecuteAsync_ThrowsWhenTableNull()
    {
        var builder = new InsertBatchQueryBuilder()
            .Row(new Dictionary<string, object>
            {
                ["tracker_id"] = 1
            });
        await using var conn = CreateConnection();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => builder.ExecuteAsync(conn));

        Assert.Equal("Tabela não especificada", ex.Message);
    }

    [Fact]
    public async Task InsertBatch_ExecuteAsync_ThrowsWhenNoRows()
    {
        var builder = new InsertBatchQueryBuilder()
            .Table("posicoes");
        await using var conn = CreateConnection();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => builder.ExecuteAsync(conn));

        Assert.Equal("Nenhuma linha para inserir", ex.Message);
    }

    [Fact]
    public void InsertBatch_ExecuteAsync_ReturnsTaskOfInt()
    {
        var builder = new InsertBatchQueryBuilder()
            .Table("posicoes")
            .Row(new Dictionary<string, object>
            {
                ["tracker_id"] = 1,
                ["status"] = "online"
            });
        using var conn = CreateConnection();

        var task = builder.ExecuteAsync(conn);

        Assert.NotNull(task);
        Assert.IsAssignableFrom<Task<int>>(task);
    }

    [Fact]
    public void InsertBatch_Row_ThrowsWhenNull()
    {
        var builder = new InsertBatchQueryBuilder().Table("posicoes");

        var ex = Assert.Throws<ArgumentNullException>(() => builder.Row(null!));

        Assert.Equal("fields", ex.ParamName);
    }

    [Fact]
    public void InsertBatch_Row_ThrowsWhenEmpty()
    {
        var builder = new InsertBatchQueryBuilder().Table("posicoes");

        var ex = Assert.Throws<InvalidOperationException>(() => builder.Row(new Dictionary<string, object>()));

        Assert.Equal("A linha do batch não pode estar vazia.", ex.Message);
    }

    [Fact]
    public void InsertBatch_Rows_ThrowsWhenNull()
    {
        var builder = new InsertBatchQueryBuilder().Table("posicoes");

        var ex = Assert.Throws<ArgumentNullException>(() => builder.Rows((IEnumerable<Dictionary<string, object>>)null!));

        Assert.Equal("rows", ex.ParamName);
    }

    [Fact]
    public void InsertBatch_RowsWithMapper_ThrowsWhenItemsNull()
    {
        var builder = new InsertBatchQueryBuilder().Table("posicoes");

        var ex = Assert.Throws<ArgumentNullException>(() =>
            builder.Rows<int>(null!, item => new Dictionary<string, object> { ["id"] = item }));

        Assert.Equal("items", ex.ParamName);
    }

    [Fact]
    public void InsertBatch_RowsWithMapper_ThrowsWhenMapNull()
    {
        var builder = new InsertBatchQueryBuilder().Table("posicoes");

        var ex = Assert.Throws<ArgumentNullException>(() =>
            builder.Rows(new[] { 1, 2 }, null!));

        Assert.Equal("map", ex.ParamName);
    }

    [Fact]
    public void InsertBatch_OnDuplicateKeyUpdate_ThrowsWhenFieldsNull()
    {
        var builder = new InsertBatchQueryBuilder().Table("posicoes");

        var ex = Assert.Throws<ArgumentNullException>(() => builder.OnDuplicateKeyUpdate(null!));

        Assert.Equal("fields", ex.ParamName);
    }

    [Fact]
    public void InsertBatch_OnDuplicateKeyUpdate_ThrowsWhenFieldsEmpty()
    {
        var builder = new InsertBatchQueryBuilder().Table("posicoes");

        var ex = Assert.Throws<InvalidOperationException>(() => builder.OnDuplicateKeyUpdate());

        Assert.Equal("Informe ao menos um campo para atualizar em caso de chave duplicada.", ex.Message);
    }

    [Fact]
    public void InsertBatch_Build_ThrowsWhenDuplicateUpdateFieldDoesNotExist()
    {
        var builder = new InsertBatchQueryBuilder()
            .Table("posicoes")
            .Row(new Dictionary<string, object>
            {
                ["tracker_id"] = 1,
                ["status"] = "online"
            })
            .OnDuplicateKeyUpdate("speed");

        var ex = Assert.Throws<InvalidOperationException>(() => builder.Build());

        Assert.Equal("O campo 'speed' não existe nas linhas do batch.", ex.Message);
    }

    [Fact]
    public void InsertBatch_Build_ThrowsWhenAllFieldsExcludedFromDuplicateUpdate()
    {
        var builder = new InsertBatchQueryBuilder()
            .Table("posicoes")
            .Row(new Dictionary<string, object>
            {
                ["tracker_id"] = 1,
                ["status"] = "online"
            })
            .OnDuplicateKeyUpdateAllExcept("tracker_id", "status");

        var ex = Assert.Throws<InvalidOperationException>(() => builder.Build());

        Assert.Equal("Nenhum campo restou para atualizar em caso de chave duplicada.", ex.Message);
    }

    [Fact]
    public void InsertBatch_RowFrom_ThrowsWhenEntityNull()
    {
        var builder = InsertBatchQueryBuilder.For<BatchLog>();

        var ex = Assert.Throws<ArgumentNullException>(() => builder.RowFrom(null!));

        Assert.Equal("entity", ex.ParamName);
    }

    [Fact]
    public void InsertBatch_RowsFrom_ThrowsWhenEntitiesNull()
    {
        var builder = InsertBatchQueryBuilder.For<BatchLog>();

        var ex = Assert.Throws<ArgumentNullException>(() => builder.RowsFrom(null!));

        Assert.Equal("entities", ex.ParamName);
    }

    [Fact]
    public void InsertBatch_RowFrom_SkipsNullsAndIgnoredMembers()
    {
        var builder = InsertBatchQueryBuilder.For<BatchLog>()
            .RowFrom(new BatchLog
            {
                TrackerId = 5,
                Status = null,
                IgnoredField = "nao deve ir"
            });

        var (sql, command) = builder.Build();

        Assert.Contains("INSERT INTO `batch_logs` (`tracker_id`) VALUES (@p0)", sql);
        Assert.DoesNotContain("status", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ignored_field", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Single(command.Parameters);
        Assert.Equal(5, command.Parameters["@p0"].Value);
    }

    [Fact]
    public void InsertBatch_RowAsJson_SerializesValue()
    {
        var builder = InsertBatchQueryBuilder.For<BatchLog>()
            .RowAsJson("Status", new { online = true });

        var (sql, command) = builder.Build();

        Assert.Contains("INSERT INTO `batch_logs` (`status`) VALUES (@p0)", sql);
        Assert.Equal("{\"online\":true}", command.Parameters["@p0"].Value);
    }

    [Fact]
    public async Task Delete_ExecuteAsync_ThrowsWhenConnectionNull()
    {
        var builder = new DeleteQueryBuilder()
            .Table("veiculos")
            .Where("id", 1);

        var ex = await Assert.ThrowsAsync<ArgumentNullException>(() => builder.ExecuteAsync(null!));

        Assert.Equal("connection", ex.ParamName);
    }

    [Fact]
    public async Task Delete_ExecuteAsync_ThrowsWhenTableNull()
    {
        var builder = new DeleteQueryBuilder()
            .Where("id", 1);
        await using var conn = CreateConnection();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => builder.ExecuteAsync(conn));

        Assert.Equal("Tabela não especificada", ex.Message);
    }

    [Fact]
    public void Delete_ExecuteAsync_ReturnsTask()
    {
        var builder = new DeleteQueryBuilder()
            .Table("veiculos")
            .Where("id", 1);
        using var conn = CreateConnection();

        var task = builder.ExecuteAsync(conn);

        Assert.NotNull(task);
        Assert.IsAssignableFrom<Task>(task);
    }

    [Fact]
    public void Delete_From_SetsTableAlias()
    {
        var builder = new DeleteQueryBuilder()
            .From("eventos")
            .Where("id", 1);

        var (sql, _) = builder.Build();

        Assert.Contains("DELETE FROM `eventos` WHERE `id` = @p0", sql);
    }

    [Fact]
    public void Delete_OrWhere_QueryOperator_GeneratesExpectedSql()
    {
        var builder = new DeleteQueryBuilder()
            .Table("logs")
            .Where("created_at", "2024-01-01", QueryOperator.GreaterThan)
            .OrWhere("created_at", "2023-01-01", QueryOperator.LessThanOrEqual);

        var (sql, command) = builder.Build();

        Assert.Contains("WHERE `created_at` > @p0 OR `created_at` <= @p1", sql);
        Assert.Equal(2, command.Parameters.Count);
    }

    [Fact]
    public void Delete_BuildSelect_ThrowsWhenTableMissing()
    {
        var builder = new DeleteQueryBuilder()
            .Where("id", 1);

        var ex = Assert.Throws<InvalidOperationException>(() => builder.BuildSelect());

        Assert.Equal("Tabela não especificada", ex.Message);
    }
}
