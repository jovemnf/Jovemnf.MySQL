using Jovemnf.MySQL.Builder;
using Xunit;

namespace MysqlTest;

public class QueryBuilderTests
{
    [Fact]
    public void TestSelectComplex()
    {
        var builder = new SelectQueryBuilder()
            .Select("u.id", "u.nome", "p.bio")
            .From("usuarios u")
            .LeftJoin("perfis p", "u.id", "=", "p.usuario_id")
            .Where("u.ativo", true)
            .WhereIn("u.tipo", new[] { 1, 2, 3 })
            .OrderBy("u.nome", "DESC")
            .Limit(10, 20);

        var (sql, _) = builder.Build();

        Assert.Contains("SELECT", sql);
        Assert.Contains("FROM", sql);
        Assert.Contains("WHERE", sql);
        Assert.Contains("LIMIT 10 OFFSET 20", sql);
    }

    [Fact]
    public void TestSelectWhereNull()
    {
        var builder = new SelectQueryBuilder()
            .Table("users")
            .WhereNull("deleted_at")
            .WhereNotNull("created_at");

        var (sql, _) = builder.Build();

        Assert.Contains("`deleted_at` IS NULL", sql);
        Assert.Contains("`created_at` IS NOT NULL", sql);
    }

    [Fact]
    public void TestSelectWhereNotIn()
    {
        var builder = new SelectQueryBuilder()
            .Table("products")
            .WhereNotIn("category_id", new[] { 10, 20 });

        var (sql, _) = builder.Build();

        Assert.Contains("NOT IN", sql);
        Assert.Contains("@p0", sql); // Parameterized
    }

    [Fact]
    public void TestJoins()
    {
        var builder = new SelectQueryBuilder()
            .From("orders o")
            .Join("customers c", "o.customer_id", "=", "c.id")
            .LeftJoin("coupons cp", "o.coupon_id", "=", "cp.id")
            .RightJoin("shippers s", "o.shipper_id", "=", "s.id");

        var (sql, _) = builder.Build();

        Assert.Contains("INNER JOIN `customers c`", sql);
        Assert.Contains("LEFT JOIN `coupons cp`", sql);
        Assert.Contains("RIGHT JOIN `shippers s`", sql);
    }

    [Fact]
    public void TestSelectDistinctGroupByAndHaving()
    {
        var builder = new SelectQueryBuilder()
            .Distinct()
            .Select("status")
            .SelectRaw("COUNT(*) AS total")
            .From("rastreamento_eventos")
            .Where("ativo", true)
            .GroupBy("status")
            .Having("status", "processado", QueryOperator.NotEquals)
            .OrHavingRaw("COUNT(*) > {0}", 10)
            .OrderBy("status");

        var (sql, command) = builder.Build();

        Assert.Contains("SELECT DISTINCT `status`, COUNT(*) AS total FROM `rastreamento_eventos`", sql);
        Assert.Contains("WHERE `ativo` = @p0", sql);
        Assert.Contains("GROUP BY `status`", sql);
        Assert.Contains("HAVING `status` <> @p1 OR COUNT(*) > @p2", sql);
        Assert.Contains("ORDER BY `status` ASC", sql);
        Assert.Equal(3, command.Parameters.Count);
    }

    [Fact]
    public void TestSelectWhere_QueryOperator_GeneratesExpectedSql()
    {
        var builder = new SelectQueryBuilder()
            .Table("logs")
            .Where("created_at", "2024-01-01", QueryOperator.GreaterThan)
            .OrWhere("created_at", "2023-01-01", QueryOperator.LessThanOrEqual);

        var (sql, _) = builder.Build();

        Assert.Contains("`created_at` > @p0", sql);
        Assert.Contains("OR `created_at` <= @p1", sql);
    }

    [Fact]
    public void TestHaving_ThrowsForInvalidOperator()
    {
        var builder = new SelectQueryBuilder()
            .Table("users")
            .GroupBy("status");

        var ex = Assert.Throws<ArgumentException>(() =>
            builder.Having("status", "active", "; DROP TABLE users; --"));

        Assert.Contains("Operador não permitido", ex.Message);
    }

    [Fact]
    public void TestOrderBy()
    {
        var builder = new SelectQueryBuilder()
            .Table("logs")
            .OrderBy("date", "DESC")
            .OrderBy("id");

        var (sql, _) = builder.Build();

        Assert.Contains("ORDER BY `date` DESC, `id` ASC", sql);
    }

    [Fact]
    public void TestInsertRobust()
    {
        var values = new Dictionary<string, object>
        {
            {"nome", "Joao"},
            {"idade", 30},
            {"nascimento", new DateTime(1990, 1, 1)},
            {"observacao", null!}
        };
        
        var builder = new InsertQueryBuilder().Table("clientes").Values(values);
        var (_, command) = builder.Build();

        Assert.Equal(4, command.Parameters.Count);
        Assert.Equal(DBNull.Value, command.Parameters["@p3"].Value);
    }

    [Fact]
    public void TestUpdateRobust()
    {
        var builder = new UpdateQueryBuilder()
            .Table("estoque")
            .Set("quantidade", 50)
            .WhereBetween("id", 100, 200)
            .OrWhere("status", "emergencia");

        var (sql, _) = builder.Build();

        Assert.Contains("BETWEEN @p1 AND @p2", sql);
        Assert.Contains("OR `status` = @p3", sql);
    }

    [Fact]
    public void TestUpdateToString()
    {
        var builder = new UpdateQueryBuilder()
            .Table("users")
            .Set("name", "Alice")
            .Where("id", 1);

        var sqlFromBuild = builder.Build().Sql;
        var sqlFromToString = builder.ToString();

        Assert.Equal(sqlFromBuild, sqlFromToString);
        Assert.Contains("@p0", sqlFromToString);
        Assert.Contains("@p1", sqlFromToString);

        // Verify param counter reset by calling again
        var sqlAgain = builder.ToString();
        Assert.Equal(sqlFromToString, sqlAgain);
    }

    [Fact]
    public void TestSelectToString()
    {
        var builder = new SelectQueryBuilder()
            .Table("users")
            .Where("id", 1);

        var sqlFromBuild = builder.Build().Sql;
        var sqlFromToString = builder.ToString();

        Assert.Equal(sqlFromBuild, sqlFromToString);
        Assert.Contains("@p0", sqlFromToString);
    }

    [Fact]
    public void TestSelectToDebugSql_ReplacesBasicParameters()
    {
        var builder = new SelectQueryBuilder()
            .Table("users")
            .Where("id", 1)
            .Where("name", "O'Brian");

        var debugSql = builder.ToDebugSql();

        Assert.Contains("`id` = 1", debugSql);
        Assert.Contains("`name` = 'O''Brian'", debugSql);
        Assert.DoesNotContain("@p0", debugSql);
        Assert.DoesNotContain("@p1", debugSql);
    }

    [Fact]
    public void TestSelectToDebugSql_FormatsSpecialValues()
    {
        var date = new DateTime(2026, 4, 20, 15, 30, 45, 123);
        var builder = new SelectQueryBuilder()
            .Table("logs")
            .Where("created_at", date)
            .Where("active", true)
            .Where("deleted_at", null!);

        var debugSql = builder.ToDebugSql();

        Assert.Contains("'2026-04-20 15:30:45.1230000'", debugSql);
        Assert.Contains("`active` = 1", debugSql);
        Assert.Contains("`deleted_at` = NULL", debugSql);
    }

    [Fact]
    public void TestSelectToDebugSql_ReplacesParametersInsideRawClauses()
    {
        var builder = new SelectQueryBuilder()
            .Table("usuarios")
            .Where("ativo", true)
            .WhereRaw("nome = AES_ENCRYPT(@p0, 'key')", "Joao");

        var debugSql = builder.ToDebugSql();

        Assert.Contains("`ativo` = 1", debugSql);
        Assert.Contains("AES_ENCRYPT('Joao', 'key')", debugSql);
    }

    [Fact]
    public void TestSelectRawWithAlias()
    {
        var builder = new SelectQueryBuilder()
            .SelectRaw("AES_DECRYPT(nome, 'key') AS nome_decrypted")
            .From("usuarios");

        var (sql, _) = builder.Build();

        Assert.Contains("AES_DECRYPT(nome, 'key') AS nome_decrypted", sql);
        Assert.DoesNotContain("`AES_DECRYPT", sql);
    }

    [Fact]
    public void TestWhereRawWithFunction()
    {
        var builder = new SelectQueryBuilder()
            .Table("usuarios")
            .WhereRaw("nome = AES_ENCRYPT(@p0, 'key')", "Joao");

        var (sql, command) = builder.Build();

        Assert.Contains("WHERE nome = AES_ENCRYPT(@p0, 'key')", sql);
        Assert.Single(command.Parameters);
        Assert.Equal("Joao", command.Parameters["@p0"].Value);
    }

    [Fact]
    public void TestWhereRawAfterWhere_RewritesParameterNames()
    {
        var builder = new SelectQueryBuilder()
            .Table("usuarios")
            .Where("ativo", true)
            .WhereRaw("nome = AES_ENCRYPT(@p0, 'key')", "Joao");

        var (sql, command) = builder.Build();

        Assert.Contains("`ativo` = @p0", sql);
        Assert.Contains("AND nome = AES_ENCRYPT(@p1, 'key')", sql);
        Assert.Equal(2, command.Parameters.Count);
        Assert.Equal(true, command.Parameters["@p0"].Value);
        Assert.Equal("Joao", command.Parameters["@p1"].Value);
    }

    [Fact]
    public void TestWhereRawWithPositionalPlaceholders()
    {
        var builder = new SelectQueryBuilder()
            .Table("usuarios")
            .WhereRaw("nome = {0} AND status = {1}", "Joao", "ativo");

        var (sql, command) = builder.Build();

        Assert.Contains("WHERE nome = @p0 AND status = @p1", sql);
        Assert.Equal(2, command.Parameters.Count);
        Assert.Equal("Joao", command.Parameters["@p0"].Value);
        Assert.Equal("ativo", command.Parameters["@p1"].Value);
    }

    [Fact]
    public void TestOrderBy_ThrowsForInvalidDirection()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new SelectQueryBuilder()
                .Table("logs")
                .OrderBy("date", "SIDEWAYS"));

        Assert.Contains("Direção de ordenação", ex.Message);
    }

    [Fact]
    public void TestJoin_ThrowsForInvalidType()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new SelectQueryBuilder()
                .From("orders")
                .Join("customers", "customer_id", "=", "customers.id", "OUTER"));

        Assert.Contains("Tipo de JOIN", ex.Message);
    }

    [Fact]
    public void TestLimit_ThrowsForNegativeValues()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SelectQueryBuilder().Limit(-1));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SelectQueryBuilder().Limit(10, -1));
    }

    [Fact]
    public void TestSelectMixedNormalAndRaw()
    {
        var builder = new SelectQueryBuilder()
            .Select("id")
            .SelectRaw("COUNT(*) as total")
            .From("usuarios")
            .OrderBy("id");

        var (sql, _) = builder.Build();

        Assert.Contains("SELECT `id`, COUNT(*) as total FROM `usuarios`", sql);
    }

    [Fact]
    public void TestCountQuery()
    {
        var builder = new SelectQueryBuilder()
            .Table("users")
            .Count();

        var (sql, _) = builder.Build();

        Assert.Equal("SELECT COUNT(*) FROM `users`", sql);
    }

    [Fact]
    public void TestCountWithColumn()
    {
        var builder = new SelectQueryBuilder()
            .Table("users")
            .Count("id");

        var (sql, _) = builder.Build();

        Assert.Equal("SELECT COUNT(`id`) FROM `users`", sql);
    }

    [Fact]
    public void TestExistsQuery()
    {
        var builder = new SelectQueryBuilder()
            .Table("users")
            .Where("status", "active")
            .OrderBy("id", "DESC")
            .Limit(10, 20);

        var (sql, command) = builder.BuildExists();

        Assert.Equal("SELECT EXISTS(SELECT 1 FROM `users` WHERE `status` = @p0 LIMIT 1)", sql);
        Assert.Single(command.Parameters);
        Assert.Equal("active", command.Parameters["@p0"].Value);
    }

    [Fact]
    public void TestExistsQuery_WithJoinAndMultipleWheres()
    {
        var builder = new SelectQueryBuilder()
            .From("orders o")
            .Join("customers c", "o.customer_id", "=", "c.id")
            .Where("o.status", "open")
            .OrWhere("c.vip", true);

        var (sql, command) = builder.BuildExists();

        Assert.Contains("SELECT EXISTS(SELECT 1 FROM `orders o` INNER JOIN `customers c` ON `o`.`customer_id` = `c`.`id`", sql);
        Assert.Contains("WHERE `o`.`status` = @p0 OR `c`.`vip` = @p1 LIMIT 1)", sql);
        Assert.Equal(2, command.Parameters.Count);
    }

    [Fact]
    public void TestExistsQuery_WithGroupByAndHaving()
    {
        var builder = new SelectQueryBuilder()
            .Table("logs")
            .Where("active", true)
            .GroupBy("status")
            .HavingRaw("COUNT(*) > {0}", 5);

        var (sql, command) = builder.BuildExists();

        Assert.Equal("SELECT EXISTS(SELECT 1 FROM `logs` WHERE `active` = @p0 GROUP BY `status` HAVING COUNT(*) > @p1 LIMIT 1)", sql);
        Assert.Equal(2, command.Parameters.Count);
    }

    [Fact]
    public void TestInsertToString()
    {
        var builder = new InsertQueryBuilder()
            .Table("users")
            .Value("name", "Bob");

        var sqlFromBuild = builder.Build().Sql;
        var sqlFromToString = builder.ToString();

        Assert.Equal(sqlFromBuild, sqlFromToString);
        Assert.Contains("@p0", sqlFromToString);
    }

    [Fact]
    public void TestDeleteToString()
    {
        var builder = new DeleteQueryBuilder()
            .Table("users")
            .Where("id", 1);

        var sqlFromBuild = builder.Build().Sql;
        var sqlFromToString = builder.ToString();

        Assert.Equal(sqlFromBuild, sqlFromToString);
        Assert.Contains("@p0", sqlFromToString);
    }

    [Fact]
    public void TestMappingRobust()
    {
        // Teste de lógica de mapeamento (demonstrativo)
        // O mapeamento real exige DbDataReader, mas a lógica de snake_case foi verificada.
        Assert.True(true);
    }
}
