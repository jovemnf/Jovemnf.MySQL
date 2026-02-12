using System;
using Jovemnf.MySQL;
using Xunit;

namespace MysqlTest;

public class ParsingTests
{
    [Fact]
    public void TestToBoolean()
    {
        Assert.True(TryParse.ToBoolean(1));
        Assert.True(TryParse.ToBoolean("1"));
        Assert.False(TryParse.ToBoolean(0));
        Assert.False(TryParse.ToBoolean("0"));
        Assert.False(TryParse.ToBoolean(null));
        Assert.False(TryParse.ToBoolean("invalid"));
    }

    [Fact]
    public void TestToDecimal()
    {
        Assert.Equal(10.5m, TryParse.ToDecimal(10.5));
        Assert.Equal(10.5m, TryParse.ToDecimal("10.5"));
        Assert.Equal(0m, TryParse.ToDecimal(null));
        Assert.Equal(0m, TryParse.ToDecimal("invalid"));
    }

    [Fact]
    public void TestToDouble()
    {
        Assert.Equal(10.5, TryParse.ToDouble(10.5));
        Assert.Equal(10.5, TryParse.ToDouble("10.5"));
        Assert.Equal(0.0, TryParse.ToDouble(null));
        Assert.Equal(0.0, TryParse.ToDouble("invalid"));
    }

    [Fact]
    public void TestToLong()
    {
        Assert.Equal(100L, TryParse.ToLong(100));
        Assert.Equal(100L, TryParse.ToLong("100"));
        Assert.Equal(0L, TryParse.ToLong(null));
        Assert.Equal(0L, TryParse.ToLong("invalid"));
    }

    [Fact]
    public void TestToInt32()
    {
        Assert.Equal(100, TryParse.ToInt32(100));
        Assert.Equal(100, TryParse.ToInt32("100"));
        Assert.Equal(0, TryParse.ToInt32(null));
        Assert.Equal(0, TryParse.ToInt32("invalid"));
    }
}
