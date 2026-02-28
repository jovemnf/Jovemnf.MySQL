using System;
using System.Collections.Generic;
using Jovemnf.MySQL;
using MysqlTest.Fakes;
using Xunit;

namespace MysqlTest;

public class TestModel
{
    public int Id { get; set; }
    public string Nome { get; set; } = null!;
    public bool Ativo { get; set; }
    public double Salario { get; set; }
    public DateTime DataNascimento { get; set; }
    public int? NullableInt { get; set; }
}

public class MappingTests
{
    [Fact]
    public void TestToModel_SimpleMapping()
    {
        var data = new List<Dictionary<string, object>>
        {
            new Dictionary<string, object>
            {
                { "Id", 10 },
                { "Nome", "Teste User" },
                { "Ativo", true },
                { "Salario", 1500.50 },
                { "DataNascimento", new DateTime(2000, 1, 1) },
                { "NullableInt", null! }
            }
        };

        using var reader = new MySQLReader(new FakeDataReader(data));
        reader.Read();
        
        var model = reader.ToModel<TestModel>();

        Assert.Equal(10, model.Id);
        Assert.Equal("Teste User", model.Nome);
        Assert.True(model.Ativo);
        Assert.Equal(1500.50, model.Salario);
        Assert.Equal(new DateTime(2000, 1, 1), model.DataNascimento);
        Assert.Null(model.NullableInt);
    }

    [Fact]
    public void TestToModel_SnakeCaseMapping()
    {
        var data = new List<Dictionary<string, object>>
        {
            new Dictionary<string, object>
            {
                { "id", 20 },
                { "nome", "Snake Case" },
                { "ativo", 1 }, // MySQL boolean is often tinyint(1)
                { "salario", 2000.00 },
                { "data_nascimento", new DateTime(1995, 5, 5) },
                { "nullable_int", 99 }
            }
        };

        using var reader = new MySQLReader(new FakeDataReader(data));
        reader.Read();
        
        var model = reader.ToModel<TestModel>();

        Assert.Equal(20, model.Id);
        Assert.Equal("Snake Case", model.Nome);
        Assert.True(model.Ativo); // Should handle int -> bool conversion
        Assert.Equal(2000.00, model.Salario);
        Assert.Equal(new DateTime(1995, 5, 5), model.DataNascimento);
        Assert.Equal(99, model.NullableInt);
    }

    [Fact]
    public void TestToModel_DbFieldMapping()
    {
        var data = new List<Dictionary<string, object>>
        {
            new Dictionary<string, object>
            {
                { "user_id", 30 },
                { "fullname", "Attribute User" }
            }
        };

        using var reader = new MySQLReader(new FakeDataReader(data));
        reader.Read();
        
        var model = reader.ToModel<ModelWithAttributes>();

        Assert.Equal(30, model.Id);
        Assert.Equal("Attribute User", model.Name);
    }

    [Fact]
    public void TestToModel_NumericMapping()
    {
        var guid = Guid.NewGuid();
        var data = new List<Dictionary<string, object>>
        {
            new Dictionary<string, object>
            {
                { "IntVal", 123 },
                { "LongVal", 1234567890L },
                { "FloatVal", 123.45f },
                { "DoubleVal", 678.90 },
                { "DecimalVal", 1000.50m },
                { "ShortVal", (short)10 },
                { "ByteVal", (byte)255 },
                { "GuidVal", guid.ToString() }
            }
        };

        using var reader = new MySQLReader(new FakeDataReader(data));
        reader.Read();
        
        var model = reader.ToModel<NumericTestModel>();

        Assert.Equal(123, model.IntVal);
        Assert.Equal(1234567890L, model.LongVal);
        Assert.Equal(123.45f, model.FloatVal);
        Assert.Equal(678.90, model.DoubleVal);
        Assert.Equal(1000.50m, model.DecimalVal);
        Assert.Equal(10, model.ShortVal);
        Assert.Equal(255, model.ByteVal);
        Assert.Equal(guid, model.GuidVal);
    }
}

public class ModelWithAttributes
{
    [DbField("user_id")]
    public int Id { get; set; }

    [DbField("fullname")]
    public string Name { get; set; } = null!;
}

public class NumericTestModel
{
    public int IntVal { get; set; }
    public long LongVal { get; set; }
    public float FloatVal { get; set; }
    public double DoubleVal { get; set; }
    public decimal DecimalVal { get; set; }
    public short ShortVal { get; set; }
    public byte ByteVal { get; set; }
    public Guid GuidVal { get; set; }
}

public class Boleto
{
    public int Id { get; set; }
    public int IdEmpresa { get; set; }
    public string Descricao { get; set; } = null!;
}

public class QueryBuilderMappingTests
{
    [Fact]
    public void TestSelectQueryBuilder_ResolvesPascalCaseToSnakeCase()
    {
        var sql = Jovemnf.MySQL.Builder.SelectQueryBuilder.For<Boleto>()
            .Where("IdEmpresa", 123)
            .ToString();

        // Should use id_empresa, not IdEmpresa or Id_Empresa
        Assert.Contains("`id_empresa` =", sql);
    }
    
    [Fact]
    public void TestSelectQueryBuilder_ResolvesSnakeCaseInputCorrectly()
    {
        var sql = Jovemnf.MySQL.Builder.SelectQueryBuilder.For<Boleto>()
            .Where("id_empresa", 123)
            .ToString();

        Assert.Contains("`id_empresa` =", sql);
    }
}
