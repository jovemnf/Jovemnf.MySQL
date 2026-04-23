using System;
using System.Collections.Generic;
using Jovemnf.MySQL;
using Xunit;

namespace MysqlTest;

[DbTable("veiculos")]
public sealed class VeiculoPk : Entity<VeiculoPk>
{
    [DbPrimaryKey]
    [DbField("id_veiculo")]
    public int IdVeiculo { get; set; }

    public string Placa { get; set; } = null!;
}

[DbTable("usuarios_permissoes")]
public sealed class UsuarioPermissaoPk : Entity<UsuarioPermissaoPk>
{
    [DbPrimaryKey(Order = 0)]
    [DbField("id_usuario")]
    public int IdUsuario { get; set; }

    [DbPrimaryKey(Order = 1)]
    [DbField("id_permissao")]
    public int IdPermissao { get; set; }

    public bool Ativo { get; set; }
}

[DbTable("usuarios_permissoes_rev")]
public sealed class UsuarioPermissaoPkReverseDeclared : Entity<UsuarioPermissaoPkReverseDeclared>
{
    [DbPrimaryKey(Order = 1)]
    [DbField("id_permissao")]
    public int IdPermissao { get; set; }

    [DbPrimaryKey(Order = 0)]
    [DbField("id_usuario")]
    public int IdUsuario { get; set; }
}

[DbTable("sem_pk")]
public sealed class EntidadeSemPk : Entity<EntidadeSemPk>
{
    public int Id { get; set; }
}

public class PrimaryKeyTests
{
    [Fact]
    public void PrimaryKeyColumns_Simple_ReturnsSingleColumn()
    {
        Assert.False(VeiculoPk.HasCompositePrimaryKey);
        Assert.Equal(new[] { "id_veiculo" }, VeiculoPk.PrimaryKeyColumns);
    }

    [Fact]
    public void PrimaryKeyColumns_Composite_RespectsDeclaredOrder()
    {
        Assert.True(UsuarioPermissaoPk.HasCompositePrimaryKey);
        Assert.Equal(new[] { "id_usuario", "id_permissao" }, UsuarioPermissaoPk.PrimaryKeyColumns);
    }

    [Fact]
    public void PrimaryKeyColumns_UseAttributeOrder_NotDeclarationOrder()
    {
        // A ordem de declaração no arquivo é [IdPermissao, IdUsuario], porém Order=0 é IdUsuario.
        Assert.Equal(new[] { "id_usuario", "id_permissao" }, UsuarioPermissaoPkReverseDeclared.PrimaryKeyColumns);
    }

    [Fact]
    public void FindByPkAsync_CompositePk_WrongArity_Throws()
    {
        // PK composta (2 colunas) + só 1 valor.
        var ex = Assert.Throws<ArgumentException>(() =>
            UsuarioPermissaoPk.FindByPkAsync((MySQL)null!, 1).GetAwaiter().GetResult());

        Assert.Contains("2 coluna(s)", ex.Message, StringComparison.Ordinal);
        Assert.Contains("1 valor(es)", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void FindByPkAsync_NoPkDeclared_Throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            EntidadeSemPk.FindByPkAsync((MySQL)null!, 1).GetAwaiter().GetResult());

        Assert.Contains("[DbPrimaryKey]", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void FindByPkAsync_Dictionary_InvalidArity_Throws()
    {
        var arityInvalid = new Dictionary<string, object> { { "id_usuario", 1 } };

        var ex = Assert.Throws<ArgumentException>(() =>
            UsuarioPermissaoPk.FindByPkAsync((MySQL)null!, arityInvalid).GetAwaiter().GetResult());

        Assert.Contains("2 coluna(s)", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void FindByPkAsync_Dictionary_MissingColumn_Throws()
    {
        // Arity certo (2) mas uma das chaves é desconhecida.
        var missingKey = new Dictionary<string, object>
        {
            { "id_usuario", 1 },
            { "coluna_que_nao_existe", 2 },
        };

        var ex = Assert.Throws<ArgumentException>(() =>
            UsuarioPermissaoPk.FindByPkAsync((MySQL)null!, missingKey).GetAwaiter().GetResult());

        Assert.Contains("id_permissao", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void FindByPkAsync_Simple_ValidArity_FailsOnlyOnNullConnection()
    {
        // Se a arity está certa, a próxima falha deve ser ArgumentNullException (conexão null).
        Assert.Throws<ArgumentNullException>(() =>
            VeiculoPk.FindByPkAsync((MySQL)null!, 42).GetAwaiter().GetResult());
    }

    [Fact]
    public void FindByPkAsync_Dictionary_AcceptsColumnOrPropertyName()
    {
        // Arity e nomes válidos (coluna do banco); deve passar da validação de PK e
        // falhar apenas no ArgumentNullException da conexão.
        var byColumn = new Dictionary<string, object>
        {
            { "id_usuario", 1 },
            { "id_permissao", 2 },
        };
        Assert.Throws<ArgumentNullException>(() =>
            UsuarioPermissaoPk.FindByPkAsync((MySQL)null!, byColumn).GetAwaiter().GetResult());

        // Mesma coisa, mas informando nomes de propriedades (case-insensitive).
        var byProperty = new Dictionary<string, object>
        {
            { nameof(UsuarioPermissaoPk.IdUsuario), 1 },
            { nameof(UsuarioPermissaoPk.IdPermissao), 2 },
        };
        Assert.Throws<ArgumentNullException>(() =>
            UsuarioPermissaoPk.FindByPkAsync((MySQL)null!, byProperty).GetAwaiter().GetResult());
    }

    [Fact]
    public void ExistsByPkAsync_WrongArity_Throws()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            UsuarioPermissaoPk.ExistsByPkAsync((MySQL)null!, 1, 2, 3).GetAwaiter().GetResult());

        Assert.Contains("2 coluna(s)", ex.Message, StringComparison.Ordinal);
        Assert.Contains("3 valor(es)", ex.Message, StringComparison.Ordinal);
    }
}
