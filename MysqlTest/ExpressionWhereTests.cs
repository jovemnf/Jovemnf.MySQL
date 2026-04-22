using System;
using System.Linq;
using Jovemnf.MySQL;
using Jovemnf.MySQL.Builder;
using Xunit;

namespace MysqlTest;

[DbTable("veiculos")]
public sealed class VeiculoWhereExpressionModel
{
    [DbField("id_cliente")]
    public int IdCliente { get; set; }

    public bool Ativo { get; set; }

    public string Status { get; set; } = null!;
}

public class ExpressionWhereTests
{
    [Fact]
    public void SelectQueryBuilder_GenericWhereExpression_GeneratesExpectedSql()
    {
        var builder = SelectQueryBuilder.For<VeiculoWhereExpressionModel>()
            .Where(v => v.IdCliente == 12 && v.Ativo && v.Status != "bloqueado");

        var (sql, command) = builder.Build();

        Assert.Contains("FROM `veiculos`", sql, StringComparison.Ordinal);
        Assert.Contains("WHERE `id_cliente` = @p0 AND `ativo` = @p1 AND `status` <> @p2", sql, StringComparison.Ordinal);
        Assert.Equal(3, command.Parameters.Count);
        Assert.Equal(12, command.Parameters["@p0"].Value);
        Assert.Equal(true, command.Parameters["@p1"].Value);
        Assert.Equal("bloqueado", command.Parameters["@p2"].Value);
    }

    [Fact]
    public void SelectQueryBuilder_GenericWhereExpression_SupportsNegationAndNull()
    {
        var builder = SelectQueryBuilder.For<VeiculoWhereExpressionModel>()
            .Where(v => !v.Ativo || v.Status == null);

        var (sql, command) = builder.Build();

        Assert.Contains("WHERE `ativo` = @p0 OR `status` IS NULL", sql, StringComparison.Ordinal);
        Assert.Single(command.Parameters);
        Assert.Equal(false, command.Parameters["@p0"].Value);
    }

    [Fact]
    public void SelectQueryBuilder_GenericWhereExpression_SupportsCollectionContains()
    {
        var ids = new[] { 10, 12, 15 };

        var builder = SelectQueryBuilder.For<VeiculoWhereExpressionModel>()
            .Where(v => ids.Contains(v.IdCliente));

        var (sql, command) = builder.Build();

        Assert.Contains("WHERE `id_cliente` IN (@p0, @p1, @p2)", sql, StringComparison.Ordinal);
        Assert.Equal(3, command.Parameters.Count);
        Assert.Equal(10, command.Parameters["@p0"].Value);
        Assert.Equal(12, command.Parameters["@p1"].Value);
        Assert.Equal(15, command.Parameters["@p2"].Value);
    }

    [Fact]
    public void SelectQueryBuilder_GenericWhereExpression_SupportsEnumerableAny()
    {
        var ids = new[] { 10, 12, 15 };

        var builder = SelectQueryBuilder.For<VeiculoWhereExpressionModel>()
            .Where(v => ids.Any(id => id == v.IdCliente));

        var (sql, command) = builder.Build();

        Assert.Contains("WHERE `id_cliente` IN (@p0, @p1, @p2)", sql, StringComparison.Ordinal);
        Assert.Equal(10, command.Parameters["@p0"].Value);
        Assert.Equal(12, command.Parameters["@p1"].Value);
        Assert.Equal(15, command.Parameters["@p2"].Value);
    }

    [Fact]
    public void SelectQueryBuilder_GenericWhereExpression_SupportsNegatedEnumerableAny()
    {
        var ids = new[] { 1, 2, 3 };

        var builder = SelectQueryBuilder.For<VeiculoWhereExpressionModel>()
            .Where(v => !ids.Any(id => id == v.IdCliente));

        var (sql, command) = builder.Build();

        Assert.Contains("WHERE `id_cliente` NOT IN (@p0, @p1, @p2)", sql, StringComparison.Ordinal);
        Assert.Equal(1, command.Parameters["@p0"].Value);
        Assert.Equal(2, command.Parameters["@p1"].Value);
        Assert.Equal(3, command.Parameters["@p2"].Value);
    }

    [Fact]
    public void SelectQueryBuilder_GenericWhereExpression_SupportsAnyWithComparison()
    {
        var ids = new[] { 10, 20 };

        var builder = SelectQueryBuilder.For<VeiculoWhereExpressionModel>()
            .Where(v => ids.Any(id => id > v.IdCliente));

        var (sql, command) = builder.Build();

        Assert.Contains("WHERE `id_cliente` < @p0 OR `id_cliente` < @p1", sql, StringComparison.Ordinal);
        Assert.Equal(10, command.Parameters["@p0"].Value);
        Assert.Equal(20, command.Parameters["@p1"].Value);
    }

    [Fact]
    public void SelectQueryBuilder_GenericWhereExpression_SupportsNegatedAnyWithComparison()
    {
        var ids = new[] { 10, 20 };

        var builder = SelectQueryBuilder.For<VeiculoWhereExpressionModel>()
            .Where(v => !ids.Any(id => id > v.IdCliente));

        var (sql, command) = builder.Build();

        Assert.Contains("WHERE NOT (`id_cliente` < @p0 OR `id_cliente` < @p1)", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void SelectQueryBuilder_GenericWhereExpression_SupportsAnyWithEmptyCollection()
    {
        var ids = Array.Empty<int>();

        var builder = SelectQueryBuilder.For<VeiculoWhereExpressionModel>()
            .Where(v => ids.Any(id => id > v.IdCliente));

        var (sql, _) = builder.Build();

        Assert.Contains("WHERE 1 = 0", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void SelectQueryBuilder_GenericWhereExpression_SupportsStringContainsAndStartsWith()
    {
        var builder = SelectQueryBuilder.For<VeiculoWhereExpressionModel>()
            .Where(v => v.Status.Contains("bloq") || v.Status.StartsWith("off"));

        var (sql, command) = builder.Build();

        Assert.Contains("WHERE `status` LIKE @p0 OR `status` LIKE @p1", sql, StringComparison.Ordinal);
        Assert.Equal("%bloq%", command.Parameters["@p0"].Value);
        Assert.Equal("off%", command.Parameters["@p1"].Value);
    }

    [Fact]
    public void SelectQueryBuilder_GenericWhereExpression_SupportsNegatedContains()
    {
        var ids = new[] { 1, 2, 3 };

        var builder = SelectQueryBuilder.For<VeiculoWhereExpressionModel>()
            .Where(v => !ids.Contains(v.IdCliente) && !v.Status.EndsWith("ado"));

        var (sql, command) = builder.Build();

        Assert.Contains("WHERE `id_cliente` NOT IN (@p0, @p1, @p2) AND `status` NOT LIKE @p3", sql, StringComparison.Ordinal);
        Assert.Equal("%ado", command.Parameters["@p3"].Value);
    }

    [Fact]
    public void SelectQueryBuilder_GenericWhereExpression_SupportsNullGuardedStringContains()
    {
        var builder = SelectQueryBuilder.For<VeiculoWhereExpressionModel>()
            .Where(v => v.Status != null && v.Status.Contains("bloq"));

        var (sql, command) = builder.Build();

        Assert.Contains("WHERE `status` IS NOT NULL AND `status` LIKE @p0", sql, StringComparison.Ordinal);
        Assert.Equal("%bloq%", command.Parameters["@p0"].Value);
    }

    [Fact]
    public void UpdateQueryBuilder_GenericWhereExpression_GeneratesExpectedSql()
    {
        var sql = UpdateQueryBuilder.For<VeiculoWhereExpressionModel>()
            .Where(v => v.IdCliente >= 10 && v.Status != "offline")
            .Set("Status", "online")
            .ToString();

        Assert.Contains("UPDATE `veiculos`", sql, StringComparison.Ordinal);
        Assert.Contains("WHERE `id_cliente` >= @p1 AND `status` <> @p2", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void DeleteQueryBuilder_GenericWhereExpression_GeneratesExpectedSql()
    {
        var sql = DeleteQueryBuilder.For<VeiculoWhereExpressionModel>()
            .Where(v => v.IdCliente == 99 || !v.Ativo)
            .ToString();

        Assert.Contains("DELETE FROM `veiculos`", sql, StringComparison.Ordinal);
        Assert.Contains("WHERE `id_cliente` = @p0 OR `ativo` = @p1", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void DeleteQueryBuilder_GenericWhereExpression_SupportsCollectionContains()
    {
        var ids = new[] { 7, 8 };

        var sql = DeleteQueryBuilder.For<VeiculoWhereExpressionModel>()
            .Where(v => ids.Contains(v.IdCliente) || v.Status.Contains("offline"))
            .ToString();

        Assert.Contains("WHERE `id_cliente` IN (@p0, @p1) OR `status` LIKE @p2", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void SelectQueryBuilder_GenericWhereExpression_SupportsGroupedLogic()
    {
        var builder = SelectQueryBuilder.For<VeiculoWhereExpressionModel>()
            .Where(v => (v.IdCliente == 12 || v.Ativo) && v.Status != "bloqueado");

        var (sql, command) = builder.Build();

        Assert.Contains("WHERE (`id_cliente` = @p0 OR `ativo` = @p1) AND `status` <> @p2", sql, StringComparison.Ordinal);
        Assert.Equal(12, command.Parameters["@p0"].Value);
        Assert.Equal(true, command.Parameters["@p1"].Value);
        Assert.Equal("bloqueado", command.Parameters["@p2"].Value);
    }
}
