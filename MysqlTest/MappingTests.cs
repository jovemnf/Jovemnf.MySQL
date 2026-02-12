using System;
using System.Collections.Generic;
using Jovemnf.MySQL;
using MysqlTest.Fakes;
using Xunit;

namespace MysqlTest;

public class TestModel
{
    public int Id { get; set; }
    public string Nome { get; set; }
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
                { "NullableInt", null }
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
}
