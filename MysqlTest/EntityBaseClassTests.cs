using System;
using Jovemnf.MySQL;
using Xunit;

namespace MysqlTest;

[DbTable("veiculos")]
public sealed class Veiculo : Entity<Veiculo>
{
    [DbField("id_veiculo")]
    public int IdVeiculo { get; set; }

    [DbField("id_cliente")]
    public int IdCliente { get; set; }

    public bool Ativo { get; set; }

    public string Status { get; set; } = null!;
}

public class EntityBaseClassTests
{
    [Fact]
    public void Entity_Where_Expression_GeneratesExpectedSql()
    {
        var builder = Veiculo.Where(v => v.IdVeiculo == 1);

        var (sql, command) = builder.Build();

        Assert.Contains("FROM `veiculos`", sql, StringComparison.Ordinal);
        Assert.Contains("WHERE `id_veiculo` = @p0", sql, StringComparison.Ordinal);
        Assert.Single(command.Parameters);
        Assert.Equal(1, command.Parameters["@p0"].Value);
    }

    [Fact]
    public void Entity_Where_CanBeChained()
    {
        var builder = Veiculo.Where(v => v.IdCliente == 10 && v.Ativo)
            .OrderBy(nameof(Veiculo.IdVeiculo), "DESC")
            .Limit(20);

        var (sql, _) = builder.Build();

        Assert.Contains("FROM `veiculos`", sql, StringComparison.Ordinal);
        Assert.Contains("WHERE `id_cliente` = @p0 AND `ativo` = @p1", sql, StringComparison.Ordinal);
        Assert.Contains("ORDER BY `id_veiculo` DESC", sql, StringComparison.Ordinal);
        Assert.Contains("LIMIT 20", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void Entity_Query_ReturnsBuilderForEntityTable()
    {
        var builder = Veiculo.Query();
        var (sql, _) = builder.Build();

        Assert.Contains("FROM `veiculos`", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void Entity_Select_RestrictsFields()
    {
        var builder = Veiculo.Select(nameof(Veiculo.IdVeiculo), nameof(Veiculo.Status));
        var (sql, _) = builder.Build();

        Assert.Contains("SELECT `id_veiculo`, `status`", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void Entity_Update_Delete_Insert_ReturnTypedBuilders()
    {
        var update = Veiculo.Update();
        var delete = Veiculo.Delete();
        var insert = Veiculo.Insert();

        Assert.NotNull(update);
        Assert.NotNull(delete);
        Assert.NotNull(insert);
    }
}
