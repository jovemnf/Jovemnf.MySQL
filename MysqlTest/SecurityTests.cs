using System;
using Jovemnf.MySQL.Builder;
using Xunit;

namespace MysqlTest;

public class SecurityTests
{
    [Fact]
    public void TestSelectIdentifierInjection()
    {
        var payload = "usuarios`; DROP TABLE backup; --";
        var builder = new SelectQueryBuilder().Table(payload);
        var (sql, _) = builder.Build();

        // O backtick no payload deve ser escapado para evitar quebra do identificador
        Assert.Contains("`usuarios``; DROP TABLE backup; --`", sql);
    }

    [Fact]
    public void TestSelectValueInjection()
    {
        var payload = "' OR 1=1 --";
        var builder = new SelectQueryBuilder()
            .From("usuarios")
            .Where("email", payload);

        var (sql, command) = builder.Build();

        Assert.Contains("WHERE `email` = @p0", sql);
        Assert.Equal(payload, command.Parameters["@p0"].Value?.ToString());
    }

    [Fact]
    public void TestUpdateOperatorValidation()
    {
        var builder = new UpdateQueryBuilder()
            .Table("usuarios")
            .Set("ativo", false);
            
        Assert.Throws<ArgumentException>(() => 
            builder.Where("id", 1, "= 1; DROP TABLE users; --")
        );
    }

    [Fact]
    public void TestInsertIdentifierInjection()
    {
        var payload = "coluna`; EXPLOIT --";
        var builder = new InsertQueryBuilder()
            .Table("usuarios")
            .Value(payload, "valor");

        var (sql, _) = builder.Build();

        Assert.Contains($"`{payload.Replace("`", "``")}`", sql);
    }
}
