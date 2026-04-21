using System;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Jovemnf.MySQL;
using Jovemnf.MySQL.Builder;
using Jovemnf.MySQL.Configuration;
using Xunit;

namespace MysqlTest;

public class SelectQueryBuilderCoverageTests
{
    private static MySQL CreateConnection() => new MySQL(new MySQLConfiguration
    {
        Host = "localhost",
        Database = "test",
        Username = "root",
        Password = "password"
    });

    private sealed class NoPublicPropertiesType
    {
        private int Id { get; set; }
    }

    private enum StatusEnum
    {
        Inactive = 0,
        Active = 1
    }

    private sealed record SelectionRecord(
        [property: JsonPropertyName("nome_portal")] string NomePortal,
        [property: JsonPropertyName("placa")] string Placa);

    [Fact]
    public async Task Select_ExecuteAsync_ThrowsWhenConnectionNull()
    {
        var builder = new SelectQueryBuilder()
            .Table("veiculos");

        var ex = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            builder.ExecuteAsync(null!));

        Assert.Equal("connection", ex.ParamName);
    }

    [Fact]
    public async Task Select_ExecuteAsync_ThrowsWhenTableNull()
    {
        var builder = new SelectQueryBuilder();
        await using var conn = CreateConnection();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            builder.ExecuteAsync(conn));

        Assert.Equal("Tabela não especificada", ex.Message);
    }

    [Fact]
    public void Select_ExecuteAsync_ReturnsTask()
    {
        var builder = new SelectQueryBuilder()
            .Table("veiculos")
            .Where("id", 1);
        using var conn = CreateConnection();

        var task = builder.ExecuteAsync(conn);

        Assert.NotNull(task);
        Assert.IsAssignableFrom<Task>(task);
    }

    [Fact]
    public void Select_ExistsSync_ThrowsWhenConnectionNull()
    {
        var builder = new SelectQueryBuilder()
            .Table("veiculos");

        var ex = Assert.Throws<ArgumentNullException>(() => builder.ExistsSync(null!));

        Assert.Equal("connection", ex.ParamName);
    }

    [Fact]
    public void Select_ExistsSync_ThrowsWhenTableNull()
    {
        var builder = new SelectQueryBuilder();
        using var conn = CreateConnection();

        var ex = Assert.Throws<InvalidOperationException>(() => builder.ExistsSync(conn));

        Assert.Equal("Tabela não especificada", ex.Message);
    }

    [Fact]
    public void Select_Build_ThrowsWhenTableMissing()
    {
        var builder = new SelectQueryBuilder()
            .Where("id", 1);

        var ex = Assert.Throws<InvalidOperationException>(() => builder.Build());

        Assert.Equal("Nome da tabela não definido", ex.Message);
    }

    [Fact]
    public void Select_BuildExists_ThrowsWhenTableMissing()
    {
        var builder = new SelectQueryBuilder()
            .Where("id", 1);

        var ex = Assert.Throws<InvalidOperationException>(() => builder.BuildExists());

        Assert.Equal("Nome da tabela não definido", ex.Message);
    }

    [Fact]
    public void Select_SelectInstance_ThrowsWhenNull()
    {
        var builder = new SelectQueryBuilder().Table("veiculos");

        var ex = Assert.Throws<ArgumentNullException>(() => builder.Select<SelectionRecord>(null!));

        Assert.Equal("selection", ex.ParamName);
    }

    [Fact]
    public void Select_SelectType_ThrowsWhenNull()
    {
        var builder = new SelectQueryBuilder().Table("veiculos");

        var ex = Assert.Throws<ArgumentNullException>(() => builder.Select((Type)null!));

        Assert.Equal("selectionType", ex.ParamName);
    }

    [Fact]
    public void Select_SelectType_ThrowsWhenTypeHasNoPublicProperties()
    {
        var builder = new SelectQueryBuilder().Table("veiculos");

        var ex = Assert.Throws<ArgumentException>(() => builder.Select(typeof(NoPublicPropertiesType)));

        Assert.Contains("não possui propriedades públicas", ex.Message);
        Assert.Equal("selectionType", ex.ParamName);
    }

    [Fact]
    public void Select_Count_ClearsPreviousSelectedFields()
    {
        var builder = new SelectQueryBuilder()
            .Table("users")
            .Select("id", "name")
            .Count("id");

        var (sql, _) = builder.Build();

        Assert.Equal("SELECT COUNT(`id`) FROM `users`", sql);
        Assert.DoesNotContain("`name`", sql);
    }

    [Fact]
    public void Select_OrWhereRaw_GeneratesExpectedSql()
    {
        var builder = new SelectQueryBuilder()
            .Table("usuarios")
            .Where("ativo", true)
            .OrWhereRaw("nome = {0} OR email = {1}", "Joao", "joao@teste.com");

        var (sql, command) = builder.Build();

        Assert.Contains("WHERE `ativo` = @p0 OR nome = @p1 OR email = @p2", sql);
        Assert.Equal(3, command.Parameters.Count);
        Assert.Equal("Joao", command.Parameters["@p1"].Value);
        Assert.Equal("joao@teste.com", command.Parameters["@p2"].Value);
    }

    [Fact]
    public void Select_Where_ThrowsForInvalidOperator()
    {
        var builder = new SelectQueryBuilder()
            .Table("users");

        var ex = Assert.Throws<ArgumentException>(() =>
            builder.Where("id", 1, "; DROP TABLE users; --"));

        Assert.Contains("Operador não permitido", ex.Message);
    }

    [Fact]
    public void Select_Join_ThrowsForInvalidOperator()
    {
        var builder = new SelectQueryBuilder()
            .From("orders");

        var ex = Assert.Throws<ArgumentException>(() =>
            builder.Join("customers", "customer_id", ";", "customers.id"));

        Assert.Contains("Operador não permitido", ex.Message);
    }

    [Fact]
    public void Select_ToDebugSql_FormatsEnumGuidByteArrayAndDateTimeOffset()
    {
        var guid = Guid.Parse("7e6f4b5a-3268-4ef3-a92d-6e04d6f8f0d2");
        var timestamp = new DateTimeOffset(2026, 4, 20, 8, 30, 15, TimeSpan.FromHours(-3));
        var payload = new byte[] { 0x01, 0xAF, 0x10 };

        var debugSql = new SelectQueryBuilder()
            .Table("logs")
            .Where("status", StatusEnum.Active)
            .Where("tracking_id", guid)
            .Where("payload", payload)
            .Where("created_at", timestamp)
            .ToDebugSql();

        Assert.Contains("`status` = 1", debugSql);
        Assert.Contains($"`tracking_id` = '{guid}'", debugSql);
        Assert.Contains("`payload` = 0x01AF10", debugSql);
        Assert.Contains("`created_at` = '2026-04-20 08:30:15.0000000 -03:00'", debugSql);
    }
}
