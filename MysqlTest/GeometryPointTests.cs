using System;
using System.Collections.Generic;
using Jovemnf.MySQL;
using Jovemnf.MySQL.Geometry;
using Jovemnf.MySQL.Builder;
using MysqlTest.Fakes;
using Xunit;

namespace MysqlTest;

public class GeometryPointTests
{
    [Fact]
    public void TestPoint_Constructor()
    {
        // Arrange & Act
        var point = new Point(-23.551, -46.633);

        // Assert
        Assert.Equal(-23.551, point.Latitude);
        Assert.Equal(-46.633, point.Longitude);
        Assert.Equal(4326, point.SRID);
    }

    [Fact]
    public void TestPoint_ConstructorWithSRID()
    {
        // Arrange & Act
        var point = new Point(-23.551, -46.633, 3857);

        // Assert
        Assert.Equal(-23.551, point.Latitude);
        Assert.Equal(-46.633, point.Longitude);
        Assert.Equal(3857, point.SRID);
    }

    [Fact]
    public void TestPoint_ToWKB()
    {
        // Arrange
        var point = new Point(-23.551, -46.633, 4326);

        // Act
        var wkb = point.ToWKB();

        // Assert
        Assert.NotNull(wkb);
        Assert.Equal(25, wkb.Length); // 4 (SRID) + 1 (byte order) + 4 (type) + 8 (X) + 8 (Y)
        
        // Verify SRID (first 4 bytes)
        int srid = BitConverter.ToInt32(wkb, 0);
        Assert.Equal(4326, srid);
        
        // Verify byte order
        Assert.Equal(1, wkb[4]); // Little-endian
        
        // Verify geometry type (POINT = 1)
        int geomType = BitConverter.ToInt32(wkb, 5);
        Assert.Equal(1, geomType);
        
        // Verify X (Longitude)
        double longitude = BitConverter.ToDouble(wkb, 9);
        Assert.Equal(-46.633, longitude);
        
        // Verify Y (Latitude)
        double latitude = BitConverter.ToDouble(wkb, 17);
        Assert.Equal(-23.551, latitude);
    }

    [Fact]
    public void TestPoint_FromWKB()
    {
        // Arrange
        var originalPoint = new Point(-23.551, -46.633, 4326);
        var wkb = originalPoint.ToWKB();

        // Act
        var point = Point.FromWKB(wkb);

        // Assert
        Assert.NotNull(point);
        Assert.Equal(-23.551, point.Latitude);
        Assert.Equal(-46.633, point.Longitude);
        Assert.Equal(4326, point.SRID);
    }

    [Fact]
    public void TestPoint_FromWKB_Null()
    {
        // Act
        var point = Point.FromWKB(null);

        // Assert
        Assert.Null(point);
    }

    [Fact]
    public void TestPoint_FromWKB_InvalidLength()
    {
        // Arrange
        var invalidWkb = new byte[10]; // Too short

        // Act
        var point = Point.FromWKB(invalidWkb);

        // Assert
        Assert.Null(point);
    }

    [Fact]
    public void TestPoint_RoundTrip()
    {
        // Arrange
        var originalPoint = new Point(-23.551, -46.633, 4326);

        // Act
        var wkb = originalPoint.ToWKB();
        var restoredPoint = Point.FromWKB(wkb);

        // Assert
        Assert.Equal(originalPoint.Latitude, restoredPoint.Latitude);
        Assert.Equal(originalPoint.Longitude, restoredPoint.Longitude);
        Assert.Equal(originalPoint.SRID, restoredPoint.SRID);
    }

    [Fact]
    public void TestPoint_Equality()
    {
        // Arrange
        var point1 = new Point(-23.551, -46.633, 4326);
        var point2 = new Point(-23.551, -46.633, 4326);
        var point3 = new Point(-22.0, -45.0, 4326);

        // Assert
        Assert.True(point1.Equals(point2));
        Assert.False(point1.Equals(point3));
        Assert.False(point1.Equals(null));
        Assert.False(point1.Equals("not a point"));
    }

    [Fact]
    public void TestPoint_ToString()
    {
        // Arrange
        var point = new Point(-23.551, -46.633);

        // Act
        var str = point.ToString();

        // Assert
        Assert.Equal("POINT(-23.551, -46.633)", str);
    }

    [Fact]
    public void TestInsertBuilder_ValueAsPoint()
    {
        // Arrange
        var point = new Point(-23.551, -46.633, 4326);
        var builder = new InsertQueryBuilder()
            .Table("locations")
            .Value("id", 1)
            .ValueAsPoint("coordinates", point);

        // Act
        var (sql, command) = builder.Build();

        // Assert
        Assert.Contains("INSERT INTO", sql);
        Assert.Contains("`locations`", sql);
        Assert.Contains("`coordinates`", sql);
        Assert.Contains("ST_GeomFromWKB(@p1, 4326)", sql);
        
        // Verify parameter is WKB
        var param = command.Parameters["@p1"];
        Assert.NotNull(param);
        Assert.IsType<byte[]>(param.Value);
        
        var wkb = (byte[])param.Value;
        Assert.Equal(25, wkb.Length);
    }

    [Fact]
    public void TestInsertBuilder_ValueAsPoint_Null()
    {
        // Arrange
        var builder = new InsertQueryBuilder()
            .Table("locations")
            .Value("id", 1)
            .ValueAsPoint("coordinates", null);

        // Act
        var (sql, command) = builder.Build();

        // Assert
        var param = command.Parameters["@p1"];
        Assert.NotNull(param);
        Assert.Equal(DBNull.Value, param.Value);
    }

    [Fact]
    public void TestUpdateBuilder_SetAsPoint()
    {
        // Arrange
        var point = new Point(-22.9068, -43.1729, 4326);
        var builder = new UpdateQueryBuilder()
            .Table("locations")
            .SetAsPoint("coordinates", point)
            .Where("id", 1);

        // Act
        var (sql, command) = builder.Build();

        // Assert
        Assert.Contains("UPDATE", sql);
        Assert.Contains("`locations`", sql);
        Assert.Contains("SET `coordinates` = ST_GeomFromWKB(@p0, 4326)", sql);
        Assert.Contains("WHERE", sql);
        
        // Verify parameter is WKB
        var param = command.Parameters["@p0"];
        Assert.NotNull(param);
        Assert.IsType<byte[]>(param.Value);
    }

    [Fact]
    public void TestUpdateBuilder_SetAsPoint_Null()
    {
        // Arrange
        var builder = new UpdateQueryBuilder()
            .Table("locations")
            .SetAsPoint("coordinates", null)
            .Where("id", 1);

        // Act
        var (sql, command) = builder.Build();

        // Assert
        var param = command.Parameters["@p0"];
        Assert.NotNull(param);
        Assert.Equal(DBNull.Value, param.Value);
    }

    [Fact]
    public void TestToModel_PointDeserialization()
    {
        // Arrange
        var point = new Point(-23.551, -46.633, 4326);
        var wkb = point.ToWKB();
        
        var data = new List<Dictionary<string, object>>
        {
            new Dictionary<string, object>
            {
                { "Id", 1 },
                { "Name", "São Paulo Office" },
                { "Coordinates", wkb }
            }
        };

        // Act
        using var reader = new MySQLReader(new FakeDataReader(data));
        reader.Read();
        var location = reader.ToModel<LocationModel>();

        // Assert
        Assert.Equal(1, location.Id);
        Assert.Equal("São Paulo Office", location.Name);
        Assert.NotNull(location.Coordinates);
        Assert.Equal(-23.551, location.Coordinates.Latitude);
        Assert.Equal(-46.633, location.Coordinates.Longitude);
        Assert.Equal(4326, location.Coordinates.SRID);
    }

    [Fact]
    public void TestToModel_PointDeserialization_Null()
    {
        // Arrange
        var data = new List<Dictionary<string, object>>
        {
            new Dictionary<string, object>
            {
                { "Id", 1 },
                { "Name", "Test Location" },
                { "Coordinates", DBNull.Value }
            }
        };

        // Act
        using var reader = new MySQLReader(new FakeDataReader(data));
        reader.Read();
        var location = reader.ToModel<LocationModel>();

        // Assert
        Assert.Equal(1, location.Id);
        Assert.Equal("Test Location", location.Name);
        Assert.Null(location.Coordinates);
    }

    [Fact]
    public void TestInsertBuilder_MixedPointAndNormalValues()
    {
        // Arrange
        var point = new Point(-23.551, -46.633);
        var builder = new InsertQueryBuilder()
            .Table("locations")
            .Value("id", 1)
            .Value("name", "São Paulo")
            .ValueAsPoint("coordinates", point)
            .Value("created_at", DateTime.Now);

        // Act
        var (sql, command) = builder.Build();

        // Assert
        Assert.Contains("INSERT INTO", sql);
        Assert.Contains("`id`", sql);
        Assert.Contains("`name`", sql);
        Assert.Contains("`coordinates`", sql);
        Assert.Contains("`created_at`", sql);
        Assert.Contains("ST_GeomFromWKB(@p2, 4326)", sql);
        
        // Verify we have 4 parameters
        Assert.Equal(4, command.Parameters.Count);
    }

    [Fact]
    public void TestPoint_DifferentSRIDs()
    {
        // Arrange
        var point4326 = new Point(-23.551, -46.633, 4326);
        var point3857 = new Point(-23.551, -46.633, 3857);

        // Act
        var wkb4326 = point4326.ToWKB();
        var wkb3857 = point3857.ToWKB();

        // Assert
        int srid4326 = BitConverter.ToInt32(wkb4326, 0);
        int srid3857 = BitConverter.ToInt32(wkb3857, 0);
        
        Assert.Equal(4326, srid4326);
        Assert.Equal(3857, srid3857);
    }
}

// Test model
public class LocationModel
{
    public int Id { get; set; }
    public string Name { get; set; }
    public Point Coordinates { get; set; }
}
