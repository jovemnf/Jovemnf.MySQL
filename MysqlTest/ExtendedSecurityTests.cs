using System;
using System.Collections.Generic;
using Jovemnf.MySQL.Builder;
using Xunit;

namespace MysqlTest;

public class ExtendedSecurityTests
{
    [Fact]
    public void TestUpdateWithoutWhere_ShouldThrow()
    {
        var builder = new UpdateQueryBuilder()
            .Table("users")
            .Set("active", false);

        // Deve impedir update sem where (proteção contra mass update acidental)
        var ex = Assert.Throws<InvalidOperationException>(() => builder.Build());
        Assert.Equal("Nenhuma condição WHERE definida. Use .All() se realmente deseja atualizar todas as linhas.", ex.Message);
    }

    [Fact]
    public void TestUpdateAll_ShouldWork()
    {
        var builder = new UpdateQueryBuilder()
            .Table("users")
            .Set("active", false)
            .All();

        // Deve permitir update sem where pois All() foi chamado explicitamente
        var (sql, _) = builder.Build();
        Assert.DoesNotContain("WHERE", sql);
    }

    [Fact]
    public void TestUpdateWithoutFields_ShouldThrow()
    {
        var builder = new UpdateQueryBuilder()
            .Table("users")
            .Where("id", 1);

        // Deve impedir update sem campos para atualizar
        var ex = Assert.Throws<InvalidOperationException>(() => builder.Build());
        Assert.Equal("Nenhum campo para atualizar", ex.Message);
    }

    [Fact]
    public void TestUpdateWithoutTable_ShouldThrow()
    {
        var builder = new UpdateQueryBuilder()
            .Set("active", false)
            .Where("id", 1);

        var ex = Assert.Throws<InvalidOperationException>(() => builder.Build());
        Assert.Equal("Nome da tabela não definido", ex.Message);
    }

    [Fact]
    public void TestInvalidOperator_ShouldThrow()
    {
        var builder = new UpdateQueryBuilder()
            .Table("users")
            .Set("active", false);

        // Tentativa de injetar SQL via operador
        var ex = Assert.Throws<ArgumentException>(() => 
            builder.Where("id", 1, "; DROP TABLE users; --")
        );
        
        Assert.Contains("Operador não permitido", ex.Message);
    }

    [Fact]
    public void TestIdentifierEscaping_EdgeCases()
    {
        // Testando nomes de tabelas/colunas estranhos que poderiam quebrar o SQL
        var builder = new SelectQueryBuilder()
            .Select("col`una")
            .From("tab`ela")
            .Where("fiel`d", 1);

        var (sql, _) = builder.Build();

        Assert.Contains("`col``una`", sql);
        Assert.Contains("`tab``ela`", sql);
        Assert.Contains("`fiel``d`", sql);
    }
}
