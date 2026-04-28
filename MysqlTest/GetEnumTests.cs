using System;
using System.Collections.Generic;
using Jovemnf.MySQL;
using MysqlTest.Fakes;
using Xunit;

namespace MysqlTest;

public enum StatusVeiculo
{
    Inativo = 0,
    Ativo = 1,
    EmAlerta = 2
}

public class GetEnumTests
{
    [Fact]
    public void GetEnum_ComValorInteiro_RetornaEnum()
    {
        var data = new List<Dictionary<string, object>>
        {
            new Dictionary<string, object>
            {
                { "status", 1 }
            }
        };

        using var reader = new MySqlReader(new FakeDataReader(data));
        reader.Read();

        var result = reader.GetEnum<StatusVeiculo>("status");

        Assert.Equal(StatusVeiculo.Ativo, result);
    }

    [Fact]
    public void GetEnum_ComValorStringNome_RetornaEnum()
    {
        var data = new List<Dictionary<string, object>>
        {
            new Dictionary<string, object>
            {
                { "status", "EmAlerta" }
            }
        };

        using var reader = new MySqlReader(new FakeDataReader(data));
        reader.Read();

        var result = reader.GetEnum<StatusVeiculo>("status");

        Assert.Equal(StatusVeiculo.EmAlerta, result);
    }

    [Fact]
    public void GetEnum_ComValorStringNomeCaseInsensitive_RetornaEnum()
    {
        var data = new List<Dictionary<string, object>>
        {
            new Dictionary<string, object>
            {
                { "status", "ativo" }
            }
        };

        using var reader = new MySqlReader(new FakeDataReader(data));
        reader.Read();

        var result = reader.GetEnum<StatusVeiculo>("status");

        Assert.Equal(StatusVeiculo.Ativo, result);
    }

    [Fact]
    public void GetEnum_QuandoNull_LancaExcecao()
    {
        var data = new List<Dictionary<string, object>>
        {
            new Dictionary<string, object>
            {
                { "status", DBNull.Value }
            }
        };

        using var reader = new MySqlReader(new FakeDataReader(data));
        reader.Read();

        Assert.Throws<InvalidOperationException>(() => reader.GetEnum<StatusVeiculo>("status"));
    }

    [Fact]
    public void GetEnum_QuandoNullComDefault_RetornaDefault()
    {
        var data = new List<Dictionary<string, object>>
        {
            new Dictionary<string, object>
            {
                { "status", DBNull.Value }
            }
        };

        using var reader = new MySqlReader(new FakeDataReader(data));
        reader.Read();

        var result = reader.GetEnum("status", StatusVeiculo.EmAlerta);

        Assert.Equal(StatusVeiculo.EmAlerta, result);
    }

    [Fact]
    public void GetEnum_ComValorLong_RetornaEnum()
    {
        var data = new List<Dictionary<string, object>>
        {
            new Dictionary<string, object>
            {
                { "status", 2L }
            }
        };

        using var reader = new MySqlReader(new FakeDataReader(data));
        reader.Read();

        var result = reader.GetEnum<StatusVeiculo>("status");

        Assert.Equal(StatusVeiculo.EmAlerta, result);
    }

    [Fact]
    public void GetNullableEnum_ComValor_RetornaEnum()
    {
        var data = new List<Dictionary<string, object>>
        {
            new()
            {
                { "status", 0 }
            }
        };

        using var reader = new MySqlReader(new FakeDataReader(data));
        reader.Read();

        var result = reader.GetNullableEnum<StatusVeiculo>("status");

        Assert.NotNull(result);
        Assert.Equal(StatusVeiculo.Inativo, result!.Value);
    }

    [Fact]
    public void GetNullableEnum_QuandoNull_RetornaNull()
    {
        var data = new List<Dictionary<string, object>>
        {
            new Dictionary<string, object>
            {
                { "status", DBNull.Value }
            }
        };

        using var reader = new MySqlReader(new FakeDataReader(data));
        reader.Read();

        var result = reader.GetNullableEnum<StatusVeiculo>("status");

        Assert.Null(result);
    }

    [Fact]
    public void GetEnum_ComValorByte_RetornaEnum()
    {
        var data = new List<Dictionary<string, object>>
        {
            new Dictionary<string, object>
            {
                { "status", (byte)1 }
            }
        };

        using var reader = new MySqlReader(new FakeDataReader(data));
        reader.Read();

        var result = reader.GetEnum<StatusVeiculo>("status");

        Assert.Equal(StatusVeiculo.Ativo, result);
    }
}
