using System;
using System.Collections.Generic;
using Jovemnf.MySQL;
using MysqlTest.Fakes;
using Xunit;

namespace MysqlTest;

public class GetGuidTests
{
    private static readonly Guid SampleGuid = new("3f2504e0-4f89-11d3-9a0c-0305e82c3301");

    [Fact]
    public void GetGuid_ComValorGuid_RetornaGuid()
    {
        var data = new List<Dictionary<string, object>>
        {
            new()
            {
                { "id", SampleGuid }
            }
        };

        using var reader = new MySqlReader(new FakeDataReader(data));
        reader.Read();

        var result = reader.GetGuid("id");

        Assert.Equal(SampleGuid, result);
    }

    [Fact]
    public void GetGuid_ComStringComHifen_RetornaGuid()
    {
        var data = new List<Dictionary<string, object>>
        {
            new()
            {
                { "id", SampleGuid.ToString() }
            }
        };

        using var reader = new MySqlReader(new FakeDataReader(data));
        reader.Read();

        var result = reader.GetGuid("id");

        Assert.Equal(SampleGuid, result);
    }

    [Fact]
    public void GetGuid_ComStringSemHifen_RetornaGuid()
    {
        var data = new List<Dictionary<string, object>>
        {
            new()
            {
                { "id", SampleGuid.ToString("N") }
            }
        };

        using var reader = new MySqlReader(new FakeDataReader(data));
        reader.Read();

        var result = reader.GetGuid("id");

        Assert.Equal(SampleGuid, result);
    }

    [Fact]
    public void GetGuid_ComStringComChaves_RetornaGuid()
    {
        var data = new List<Dictionary<string, object>>
        {
            new()
            {
                { "id", SampleGuid.ToString("B") }
            }
        };

        using var reader = new MySqlReader(new FakeDataReader(data));
        reader.Read();

        var result = reader.GetGuid("id");

        Assert.Equal(SampleGuid, result);
    }

    [Fact]
    public void GetGuid_ComBinary16_RetornaGuid()
    {
        var data = new List<Dictionary<string, object>>
        {
            new()
            {
                { "id", SampleGuid.ToByteArray() }
            }
        };

        using var reader = new MySqlReader(new FakeDataReader(data));
        reader.Read();

        var result = reader.GetGuid("id");

        Assert.Equal(SampleGuid, result);
    }

    [Fact]
    public void GetGuid_QuandoNull_LancaExcecao()
    {
        var data = new List<Dictionary<string, object>>
        {
            new()
            {
                { "id", DBNull.Value }
            }
        };

        using var reader = new MySqlReader(new FakeDataReader(data));
        reader.Read();

        Assert.Throws<InvalidOperationException>(() => reader.GetGuid("id"));
    }

    [Fact]
    public void GetGuid_QuandoStringInvalida_LancaExcecao()
    {
        var data = new List<Dictionary<string, object>>
        {
            new()
            {
                { "id", "nao-eh-um-guid" }
            }
        };

        using var reader = new MySqlReader(new FakeDataReader(data));
        reader.Read();

        Assert.Throws<InvalidOperationException>(() => reader.GetGuid("id"));
    }

    [Fact]
    public void GetGuid_ComColunaInvalida_LancaExcecao()
    {
        var data = new List<Dictionary<string, object>>
        {
            new()
            {
                { "id", SampleGuid }
            }
        };

        using var reader = new MySqlReader(new FakeDataReader(data));
        reader.Read();

        Assert.Throws<InvalidOperationException>(() => reader.GetGuid(""));
    }

    [Fact]
    public void GetGuidComDefault_QuandoNull_RetornaDefault()
    {
        var fallback = Guid.NewGuid();
        var data = new List<Dictionary<string, object>>
        {
            new()
            {
                { "id", DBNull.Value }
            }
        };

        using var reader = new MySqlReader(new FakeDataReader(data));
        reader.Read();

        var result = reader.GetGuid("id", fallback);

        Assert.Equal(fallback, result);
    }

    [Fact]
    public void GetGuidComDefault_QuandoStringInvalida_RetornaDefault()
    {
        var fallback = Guid.NewGuid();
        var data = new List<Dictionary<string, object>>
        {
            new()
            {
                { "id", "valor-invalido" }
            }
        };

        using var reader = new MySqlReader(new FakeDataReader(data));
        reader.Read();

        var result = reader.GetGuid("id", fallback);

        Assert.Equal(fallback, result);
    }

    [Fact]
    public void GetGuidComDefault_ComValor_RetornaValor()
    {
        var fallback = Guid.NewGuid();
        var data = new List<Dictionary<string, object>>
        {
            new()
            {
                { "id", SampleGuid }
            }
        };

        using var reader = new MySqlReader(new FakeDataReader(data));
        reader.Read();

        var result = reader.GetGuid("id", fallback);

        Assert.Equal(SampleGuid, result);
    }

    [Fact]
    public void GetNullableGuid_ComValorGuid_RetornaGuid()
    {
        var data = new List<Dictionary<string, object>>
        {
            new()
            {
                { "id", SampleGuid }
            }
        };

        using var reader = new MySqlReader(new FakeDataReader(data));
        reader.Read();

        var result = reader.GetNullableGuid("id");

        Assert.NotNull(result);
        Assert.Equal(SampleGuid, result!.Value);
    }

    [Fact]
    public void GetNullableGuid_ComString_RetornaGuid()
    {
        var data = new List<Dictionary<string, object>>
        {
            new()
            {
                { "id", SampleGuid.ToString() }
            }
        };

        using var reader = new MySqlReader(new FakeDataReader(data));
        reader.Read();

        var result = reader.GetNullableGuid("id");

        Assert.NotNull(result);
        Assert.Equal(SampleGuid, result!.Value);
    }

    [Fact]
    public void GetNullableGuid_ComBinary16_RetornaGuid()
    {
        var data = new List<Dictionary<string, object>>
        {
            new()
            {
                { "id", SampleGuid.ToByteArray() }
            }
        };

        using var reader = new MySqlReader(new FakeDataReader(data));
        reader.Read();

        var result = reader.GetNullableGuid("id");

        Assert.NotNull(result);
        Assert.Equal(SampleGuid, result!.Value);
    }

    [Fact]
    public void GetNullableGuid_QuandoNull_RetornaNull()
    {
        var data = new List<Dictionary<string, object>>
        {
            new()
            {
                { "id", DBNull.Value }
            }
        };

        using var reader = new MySqlReader(new FakeDataReader(data));
        reader.Read();

        var result = reader.GetNullableGuid("id");

        Assert.Null(result);
    }

    [Fact]
    public void GetNullableGuid_QuandoStringInvalida_RetornaNull()
    {
        var data = new List<Dictionary<string, object>>
        {
            new()
            {
                { "id", "nao-eh-um-guid" }
            }
        };

        using var reader = new MySqlReader(new FakeDataReader(data));
        reader.Read();

        var result = reader.GetNullableGuid("id");

        Assert.Null(result);
    }

    [Fact]
    public void GetNullableGuid_ComColunaVazia_RetornaNull()
    {
        var data = new List<Dictionary<string, object>>
        {
            new()
            {
                { "id", SampleGuid }
            }
        };

        using var reader = new MySqlReader(new FakeDataReader(data));
        reader.Read();

        var result = reader.GetNullableGuid("");

        Assert.Null(result);
    }
}
