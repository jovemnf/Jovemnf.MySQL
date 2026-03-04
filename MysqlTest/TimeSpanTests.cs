using System;
using System.Collections.Generic;
using Jovemnf.MySQL;
using MysqlTest.Fakes;
using Xunit;

namespace MysqlTest;

public class TimeSpanTests
{
    [Fact]
    public void GetTimeSpan_ComValor_RetornaTimeSpan()
    {
        var data = new List<Dictionary<string, object>>
        {
            new Dictionary<string, object>
            {
                { "duracao", new TimeSpan(2, 30, 15) }
            }
        };

        using var reader = new MySQLReader(new FakeDataReader(data));
        reader.Read();

        var result = reader.GetTimeSpan("duracao");

        Assert.Equal(new TimeSpan(2, 30, 15), result);
    }

    [Fact]
    public void GetTimeSpan_ComString_ConverteCorretamente()
    {
        var data = new List<Dictionary<string, object>>
        {
            new Dictionary<string, object>
            {
                { "tempo", "01:15:30" }
            }
        };

        using var reader = new MySQLReader(new FakeDataReader(data));
        reader.Read();

        var result = reader.GetTimeSpan("tempo");

        Assert.Equal(new TimeSpan(1, 15, 30), result);
    }

    [Fact]
    public void GetTimeSpan_QuandoNull_RetornaDefault()
    {
        var data = new List<Dictionary<string, object>>
        {
            new Dictionary<string, object>
            {
                { "duracao", DBNull.Value }
            }
        };

        using var reader = new MySQLReader(new FakeDataReader(data));
        reader.Read();

        var result = reader.GetTimeSpan("duracao");

        Assert.Equal(TimeSpan.Zero, result);
    }

    [Fact]
    public void GetTimeSpan_QuandoNull_RetornaDefaultCustomizado()
    {
        var customDefault = new TimeSpan(5, 0, 0);
        var data = new List<Dictionary<string, object>>
        {
            new Dictionary<string, object>
            {
                { "duracao", null! }
            }
        };

        using var reader = new MySQLReader(new FakeDataReader(data));
        reader.Read();

        var result = reader.GetTimeSpan("duracao", customDefault);

        Assert.Equal(customDefault, result);
    }

    [Fact]
    public void GetNullableTimeSpan_ComValor_RetornaTimeSpan()
    {
        var data = new List<Dictionary<string, object>>
        {
            new Dictionary<string, object>
            {
                { "duracao", new TimeSpan(3, 0, 0) }
            }
        };

        using var reader = new MySQLReader(new FakeDataReader(data));
        reader.Read();

        var result = reader.GetNullableTimeSpan("duracao");

        Assert.NotNull(result);
        Assert.Equal(new TimeSpan(3, 0, 0), result!.Value);
    }

    [Fact]
    public void GetNullableTimeSpan_QuandoNull_RetornaNull()
    {
        var data = new List<Dictionary<string, object>>
        {
            new Dictionary<string, object>
            {
                { "duracao", DBNull.Value }
            }
        };

        using var reader = new MySQLReader(new FakeDataReader(data));
        reader.Read();

        var result = reader.GetNullableTimeSpan("duracao");

        Assert.Null(result);
    }

    [Fact]
    public void ToModel_ComPropriedadeTimeSpan_MapeiaCorretamente()
    {
        var data = new List<Dictionary<string, object>>
        {
            new Dictionary<string, object>
            {
                { "Id", 1 },
                { "Duracao", new TimeSpan(2, 30, 0) },
                { "TempoOpcional", "01:15:30" }
            }
        };

        using var reader = new MySQLReader(new FakeDataReader(data));
        reader.Read();

        var model = reader.ToModel<ModeloComTimeSpan>();

        Assert.Equal(1, model.Id);
        Assert.Equal(new TimeSpan(2, 30, 0), model.Duracao);
        Assert.NotNull(model.TempoOpcional);
        Assert.Equal(new TimeSpan(1, 15, 30), model.TempoOpcional!.Value);
    }

    [Fact]
    public void ToModel_ComTimeSpanNullableNull_RetornaNull()
    {
        var data = new List<Dictionary<string, object>>
        {
            new Dictionary<string, object>
            {
                { "Id", 1 },
                { "Duracao", new TimeSpan(0, 0, 0) },
                { "TempoOpcional", DBNull.Value }
            }
        };

        using var reader = new MySQLReader(new FakeDataReader(data));
        reader.Read();

        var model = reader.ToModel<ModeloComTimeSpan>();

        Assert.Equal(1, model.Id);
        Assert.Null(model.TempoOpcional);
    }
}

public class ModeloComTimeSpan
{
    public int Id { get; set; }
    public TimeSpan Duracao { get; set; }
    public TimeSpan? TempoOpcional { get; set; }
}
