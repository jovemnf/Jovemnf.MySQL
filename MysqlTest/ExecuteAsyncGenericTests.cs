using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Jovemnf.MySQL;
using Jovemnf.MySQL.Builder;
using Jovemnf.MySQL.Configuration;
using Xunit;

namespace MysqlTest;

/// <summary>
/// Testes para ExecuteAsync&lt;T&gt; nos builders Insert, Update, Delete e Select.
/// </summary>
[Collection(MutationProtectionTestCollection.Name)]
public class ExecuteAsyncGenericTests
{
    private static MySQL CreateConnection() => new MySQL(new MySQLConfiguration
    {
        Host = "localhost",
        Database = "test",
        Username = "root",
        Password = "password"
    });

    private class DummyEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    // --- InsertQueryBuilder.ExecuteAsync<T> ---

    [Fact]
    public async Task Insert_ExecuteAsyncT_ThrowsWhenConnectionNull()
    {
        var builder = InsertQueryBuilder.For<DummyEntity>()
            .Value("name", "x");

        var ex = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            builder.ExecuteAsync<DummyEntity>(null!));

        Assert.Equal("connection", ex.ParamName);
    }

    [Fact]
    public async Task Insert_ExecuteAsyncT_ThrowsWhenTableNull()
    {
        var builder = new InsertQueryBuilder()
            .Value("name", "x");
        await using var conn = CreateConnection();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            builder.ExecuteAsync<DummyEntity>(conn));

        Assert.Equal("Tabela não especificada", ex.Message);
    }

    [Fact]
    public async Task Insert_ExecuteAsyncT_ThrowsWhenNoFields()
    {
        var builder = new InsertQueryBuilder()
            .Table("t");
        await using var conn = CreateConnection();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            builder.ExecuteAsync<DummyEntity>(conn));

        Assert.Equal("Nenhum campo para inserir", ex.Message);
    }

    [Fact]
    public void Insert_BuildSelectById_GeneratesCorrectSql()
    {
        var builder = new InsertQueryBuilder()
            .Table("veiculos")
            .Value("nome", "x");
        var (sql, cmd) = builder.BuildSelectById(123);

        Assert.Contains("SELECT * FROM `veiculos` WHERE `id` = @p0", sql);
        Assert.Single(cmd.Parameters);
        Assert.Equal(123L, Convert.ToInt64(cmd.Parameters["@p0"].Value));
    }

    // --- DeleteQueryBuilder.ExecuteAsync<T> ---

    [Fact]
    public async Task Delete_ExecuteAsyncT_ThrowsWhenConnectionNull()
    {
        var builder = new DeleteQueryBuilder()
            .Table("t")
            .Where("id", 1);

        var ex = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            builder.ExecuteAsync<DummyEntity>(null!));

        Assert.Equal("connection", ex.ParamName);
    }

    [Fact]
    public async Task Delete_ExecuteAsyncT_ThrowsWhenTableNull()
    {
        var builder = new DeleteQueryBuilder()
            .Where("id", 1);
        await using var conn = CreateConnection();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            builder.ExecuteAsync<DummyEntity>(conn));

        Assert.Equal("Tabela não especificada", ex.Message);
    }

    [Fact]
    public void Delete_BuildSelect_ThrowsWhenAllUsed_ExecuteAsyncTNotSupported()
    {
        var builder = new DeleteQueryBuilder()
            .Table("t")
            .All();

        var ex = Assert.Throws<InvalidOperationException>(() => builder.BuildSelect());

        Assert.Contains("All()", ex.Message);
        Assert.Contains("ExecuteAsync<T> não é suportado", ex.Message);
    }

    [Fact]
    public void Delete_BuildSelect_GeneratesCorrectSql()
    {
        var builder = new DeleteQueryBuilder()
            .Table("logs")
            .Where("id", 42);
        var (sql, cmd) = builder.BuildSelect();

        Assert.Contains("SELECT * FROM `logs` WHERE `id` = @p0", sql);
        Assert.Single(cmd.Parameters);
        Assert.Equal(42, cmd.Parameters["@p0"].Value);
    }

    [Fact]
    public void Delete_BuildSelect_ThrowsWhenNoWhere()
    {
        var builder = new DeleteQueryBuilder()
            .Table("t");

        var ex = Assert.Throws<InvalidOperationException>(() => builder.BuildSelect());
        Assert.Contains("WHERE", ex.Message);
    }

    [Fact]
    public void Delete_BuildSelect_ThrowsWhenAll()
    {
        var builder = new DeleteQueryBuilder()
            .Table("t")
            .All();

        var ex = Assert.Throws<InvalidOperationException>(() => builder.BuildSelect());
        Assert.Contains("All()", ex.Message);
    }

    // --- UpdateQueryBuilder.ExecuteAsync<T> ---

    [Fact]
    public async Task Update_ExecuteAsyncT_ThrowsWhenConnectionNull()
    {
        var builder = UpdateQueryBuilder.For<DummyEntity>()
            .Set("name", "y")
            .Where("id", 1);

        var ex = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            builder.ExecuteAsync<DummyEntity>(null!));

        Assert.Equal("connection", ex.ParamName);
    }

    [Fact]
    public async Task Update_ExecuteAsyncT_ThrowsWhenTableNull()
    {
        var builder = new UpdateQueryBuilder()
            .Set("name", "y")
            .Where("id", 1);
        await using var conn = CreateConnection();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            builder.ExecuteAsync<DummyEntity>(conn));

        Assert.Equal("Tabela não especificada", ex.Message);
    }

    [Fact]
    public async Task Update_ExecuteAsyncT_ThrowsWhenNoFields()
    {
        var builder = new UpdateQueryBuilder()
            .Table("t")
            .Where("id", 1);
        await using var conn = CreateConnection();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            builder.ExecuteAsync<DummyEntity>(conn));

        Assert.Equal("Nenhum campo para atualizar", ex.Message);
    }

    [Fact]
    public void Update_BuildSelect_ThrowsWhenAllUsed()
    {
        var builder = new UpdateQueryBuilder()
            .Table("t")
            .Set("name", "y")
            .All();
        builder.Build(); // deixa o builder válido para UPDATE

        var ex = Assert.Throws<InvalidOperationException>(() => builder.BuildSelect());

        Assert.Contains("All()", ex.Message);
    }

    [Fact]
    public void Update_BuildSelect_GeneratesCorrectSql()
    {
        var builder = new UpdateQueryBuilder()
            .Table("veiculos")
            .Set("status", "ativo")
            .Where("id", 10);
        builder.Build(); // consume builder for UPDATE
        var (sql, cmd) = builder.BuildSelect();

        Assert.Contains("SELECT * FROM `veiculos` WHERE `id` = @p0", sql);
        Assert.Single(cmd.Parameters);
        Assert.Equal(10, cmd.Parameters["@p0"].Value);
    }

    // --- SelectQueryBuilder.ExecuteAsync<T> ---

    [Fact]
    public async Task Select_ExecuteAsyncT_ThrowsWhenConnectionNull()
    {
        var builder = new SelectQueryBuilder()
            .Table("t")
            .Select("*");

        var ex = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            builder.ExecuteAsync<DummyEntity>(null!));

        Assert.Equal("connection", ex.ParamName);
    }

    [Fact]
    public async Task Select_ExecuteAsyncT_ThrowsWhenTableNull()
    {
        var builder = new SelectQueryBuilder()
            .Select("*");
        await using var conn = CreateConnection();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            builder.ExecuteAsync<DummyEntity>(conn));

        Assert.Equal("Tabela não especificada", ex.Message);
    }

    [Fact]
    public void Select_ExecuteAsyncT_ReturnsTaskOfListT()
    {
        var builder = new SelectQueryBuilder()
            .Table("veiculos")
            .Select("*");
        using var conn = CreateConnection();

        var task = builder.ExecuteAsync<DummyEntity>(conn);

        Assert.NotNull(task);
        Assert.IsAssignableFrom<Task<List<DummyEntity>>>(task);
    }

    [Fact]
    public async Task Select_ExistsAsync_ThrowsWhenConnectionNull()
    {
        var builder = new SelectQueryBuilder()
            .Table("t");

        var ex = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            builder.ExistsAsync(null!));

        Assert.Equal("connection", ex.ParamName);
    }

    [Fact]
    public async Task Select_ExistsAsync_ThrowsWhenTableNull()
    {
        var builder = new SelectQueryBuilder();
        await using var conn = CreateConnection();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            builder.ExistsAsync(conn));

        Assert.Equal("Tabela não especificada", ex.Message);
    }

    [Fact]
    public void Select_ExistsAsync_ReturnsTaskOfBool()
    {
        var builder = new SelectQueryBuilder()
            .Table("veiculos")
            .Where("id", 1);
        using var conn = CreateConnection();

        var task = builder.ExistsAsync(conn);

        Assert.NotNull(task);
        Assert.IsAssignableFrom<Task<bool>>(task);
    }

    // ========== Testes adicionais: BuildSelect/BuildSelectById com WHERE complexo ==========

    [Fact]
    public void Insert_BuildSelectById_WithLongId_ParameterValueCorrect()
    {
        var builder = new InsertQueryBuilder()
            .Table("rastreamento")
            .Value("placa", "ABC-1234");
        var (sql, cmd) = builder.BuildSelectById(999L);

        Assert.Contains("WHERE `id` = @p0", sql);
        Assert.Equal(999L, Convert.ToInt64(cmd.Parameters["@p0"].Value));
    }

    [Fact]
    public void Delete_BuildSelect_WithWhereIn_GeneratesCorrectSql()
    {
        var builder = new DeleteQueryBuilder()
            .Table("eventos")
            .WhereIn("veiculo_id", new[] { 10, 20, 30 });
        var (sql, cmd) = builder.BuildSelect();

        Assert.Contains("SELECT * FROM `eventos`", sql);
        Assert.Contains("`veiculo_id` IN (@p0, @p1, @p2)", sql);
        Assert.Equal(3, cmd.Parameters.Count);
        Assert.Equal(10, cmd.Parameters["@p0"].Value);
        Assert.Equal(20, cmd.Parameters["@p1"].Value);
        Assert.Equal(30, cmd.Parameters["@p2"].Value);
    }

    [Fact]
    public void Delete_BuildSelect_WithOrWhere_GeneratesCorrectSql()
    {
        var builder = new DeleteQueryBuilder()
            .Table("logs")
            .Where("tipo", "erro")
            .OrWhere("tipo", "aviso");
        var (sql, cmd) = builder.BuildSelect();

        Assert.Contains("WHERE `tipo` = @p0 OR `tipo` = @p1", sql);
        Assert.Equal(2, cmd.Parameters.Count);
        Assert.Equal("erro", cmd.Parameters["@p0"].Value);
        Assert.Equal("aviso", cmd.Parameters["@p1"].Value);
    }

    [Fact]
    public void Delete_BuildSelect_WithBetween_GeneratesCorrectSql()
    {
        var builder = new DeleteQueryBuilder()
            .Table("historico")
            .WhereBetween("data", new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));
        var (sql, cmd) = builder.BuildSelect();

        Assert.Contains("WHERE `data` BETWEEN @p0 AND @p1", sql);
        Assert.Equal(2, cmd.Parameters.Count);
    }

    [Fact]
    public void Update_BuildSelect_WithMultipleWhere_GeneratesCorrectSql()
    {
        var builder = new UpdateQueryBuilder()
            .Table("veiculos")
            .Set("status", "ativo")
            .Where("cliente_id", 5)
            .Where("ativo", true);
        builder.Build();
        var (sql, cmd) = builder.BuildSelect();

        Assert.Contains("SELECT * FROM `veiculos` WHERE `cliente_id` = @p0 AND `ativo` = @p1", sql);
        Assert.Equal(2, cmd.Parameters.Count);
        Assert.Equal(5, cmd.Parameters["@p0"].Value);
        Assert.Equal(true, cmd.Parameters["@p1"].Value);
    }

    [Fact]
    public void Update_BuildSelect_WithWhereIn_GeneratesCorrectSql()
    {
        var builder = new UpdateQueryBuilder()
            .Table("alertas")
            .Set("lido", true)
            .WhereIn("id", new[] { 1, 2, 3 });
        builder.Build();
        var (sql, cmd) = builder.BuildSelect();

        Assert.Contains("`id` IN (@p0, @p1, @p2)", sql);
        Assert.Equal(3, cmd.Parameters.Count);
    }

    [Fact]
    public void Select_ForT_ExecuteAsyncT_ReturnsTaskOfListT()
    {
        var builder = SelectQueryBuilder.For<DummyEntity>()
            .Select("*")
            .Where("Id", 1);
        using var conn = CreateConnection();

        var task = builder.ExecuteAsync<DummyEntity>(conn);

        Assert.NotNull(task);
        Assert.IsAssignableFrom<Task<List<DummyEntity>>>(task);
    }

    [Fact]
    public async Task Insert_ForT_ExecuteAsyncT_FailsWhenConnectionNotOpen()
    {
        var builder = InsertQueryBuilder.For<DummyEntity>()
            .Value("Name", "Test");
        await using var conn = CreateConnection();

        // For<T> já define tabela (GetTableName<T>), então não deve lançar "Tabela não especificada"
        var task = builder.ExecuteAsync<DummyEntity>(conn);
        Assert.NotNull(task);
        // Await falha por conexão não aberta (MySqlConnector lança InvalidOperationException)
        await Assert.ThrowsAsync<InvalidOperationException>(() => task);
    }

    [Fact]
    public async Task MySQL_ExecuteQueryAsyncT_ThrowsWhenConnectionNotOpen()
    {
        await using var conn = CreateConnection();
        var builder = new SelectQueryBuilder()
            .Table("veiculos")
            .Select("*");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            conn.ExecuteQueryAsync<DummyEntity>(builder));
    }

    [Fact]
    public async Task MySQL_ExecuteExistsAsync_ThrowsWhenConnectionNotOpen()
    {
        await using var conn = CreateConnection();
        var builder = new SelectQueryBuilder()
            .Table("veiculos")
            .Where("id", 1);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            conn.ExecuteExistsAsync(builder));
    }

    [Fact]
    public async Task MySQL_ExecuteInsertAsyncT_ThrowsWhenConnectionNotOpen()
    {
        await using var conn = CreateConnection();
        var builder = new InsertQueryBuilder()
            .Table("veiculos")
            .Value("nome", "x");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            conn.ExecuteInsertAsync<DummyEntity>(builder));
    }

    [Fact]
    public async Task MySQL_ExecuteUpdateAsyncT_ThrowsWhenConnectionNotOpen()
    {
        await using var conn = CreateConnection();
        var builder = new UpdateQueryBuilder()
            .Table("veiculos")
            .Set("nome", "y")
            .Where("id", 1);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            conn.ExecuteUpdateAsync<DummyEntity>(builder));
    }

    [Fact]
    public async Task MySQL_ExecuteDeleteAsyncT_ThrowsWhenConnectionNotOpen()
    {
        using var conn = CreateConnection();
        var builder = new DeleteQueryBuilder()
            .Table("veiculos")
            .Where("id", 1);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            conn.ExecuteDeleteAsync<DummyEntity>(builder));
    }

    /// <summary>
    /// Garante que os tipos de retorno dos ExecuteAsync&lt;T&gt; estão corretos (T vs List&lt;T&gt;).
    /// </summary>
    [Fact]
    public void ExecuteAsyncT_ReturnTypes_AreCorrect()
    {
        using var conn = CreateConnection();

        Task<DummyEntity?> insertTask = InsertQueryBuilder.For<DummyEntity>()
            .Value("Name", "x")
            .ExecuteAsync<DummyEntity>(conn);

        Task<DummyEntity?> updateTask = UpdateQueryBuilder.For<DummyEntity>()
            .Set("Name", "y")
            .Where("Id", 1)
            .ExecuteAsync<DummyEntity>(conn);

        Task<List<DummyEntity>> selectTask = SelectQueryBuilder.For<DummyEntity>()
            .Select("*")
            .ExecuteAsync<DummyEntity>(conn);

        Task<List<DummyEntity>> deleteTask = new DeleteQueryBuilder()
            .Table("t")
            .Where("id", 1)
            .ExecuteAsync<DummyEntity>(conn);

        Assert.NotNull(insertTask);
        Assert.NotNull(updateTask);
        Assert.NotNull(selectTask);
        Assert.NotNull(deleteTask);
        Assert.IsType<Task<DummyEntity?>>(insertTask);
        Assert.IsType<Task<DummyEntity?>>(updateTask);
        Assert.IsType<Task<List<DummyEntity>>>(selectTask);
        Assert.IsType<Task<List<DummyEntity>>>(deleteTask);
    }

    [Fact]
    public void Update_BuildSelect_ThrowsWhenNoWhere()
    {
        var builder = new UpdateQueryBuilder()
            .Table("t")
            .Set("x", 1);
        // Sem Where(); BuildSelect() exige pelo menos uma condição WHERE

        var ex = Assert.Throws<InvalidOperationException>(() => builder.BuildSelect());
        Assert.Contains("WHERE", ex.Message);
    }

    [Fact]
    public void Select_Build_WithForT_ProducesValidSqlForExecuteAsyncT()
    {
        var builder = SelectQueryBuilder.For<DummyEntity>()
            .Select("Id", "Name")
            .Where("Id", 10);
        var (sql, cmd) = builder.Build();

        Assert.Contains("SELECT", sql);
        Assert.Contains("FROM", sql);
        Assert.Contains("DummyEntity", sql);
        Assert.Single(cmd.Parameters);
        Assert.Equal(10, cmd.Parameters["@p0"].Value);
    }
}
