using System;
using System.Collections.Generic;
using Jovemnf.MySQL;
using Jovemnf.MySQL.Geometry;
using Jovemnf.MySQL.Builder;
using MysqlTest.Fakes;
using Xunit;

namespace MysqlTest;

public class GeometryPolygonTests
{
    [Fact]
    public void TestPolygon_Constructor()
    {
        // Arrange
        var vertices = new List<Point>
        {
            new Point(-23.5, -46.6),
            new Point(-23.5, -46.7),
            new Point(-23.6, -46.6)
        };

        // Act
        var polygon = new Polygon(vertices);

        // Assert
        Assert.Equal(3, polygon.Vertices.Count);
        Assert.Equal(4326, polygon.SRID);
    }

    [Fact]
    public void TestPolygon_ToWKT()
    {
        // Arrange
        var vertices = new List<Point>
        {
            new Point(-23.5, -46.6),
            new Point(-23.5, -46.7),
            new Point(-23.6, -46.6)
        };
        var polygon = new Polygon(vertices);

        // Act
        var wkt = polygon.ToWKT();

        // Assert
        Assert.NotNull(wkt);
        Assert.StartsWith("POLYGON((", wkt);
        Assert.EndsWith("))", wkt);
        Assert.Contains("-46.6 -23.5", wkt);
        Assert.Contains("-46.7 -23.5", wkt);
        Assert.Contains("-46.6 -23.6", wkt);
    }

    [Fact]
    public void TestPolygon_ToWKT_AutoClosesRing()
    {
        // Arrange - Not closed (first != last)
        var vertices = new List<Point>
        {
            new Point(-23.5, -46.6),
            new Point(-23.5, -46.7),
            new Point(-23.6, -46.6)
        };
        var polygon = new Polygon(vertices);

        // Act
        var wkt = polygon.ToWKT();

        // Assert - Should auto-close by adding first point at the end
        var coordCount = wkt.Split(',').Length;
        Assert.Equal(4, coordCount); // 3 original + 1 to close
    }

    [Fact]
    public void TestPolygon_ToWKT_AlreadyClosed()
    {
        // Arrange - Already closed (first == last)
        var vertices = new List<Point>
        {
            new Point(-23.5, -46.6),
            new Point(-23.5, -46.7),
            new Point(-23.6, -46.6),
            new Point(-23.5, -46.6)  // Same as first
        };
        var polygon = new Polygon(vertices);

        // Act
        var wkt = polygon.ToWKT();

        // Assert - Should NOT add another point
        var coordCount = wkt.Split(',').Length;
        Assert.Equal(4, coordCount); // Already has 4
    }

    [Fact]
    public void TestPolygon_ToWKT_MinimumVertices()
    {
        // Arrange - Less than 3 vertices
        var vertices = new List<Point>
        {
            new Point(-23.5, -46.6),
            new Point(-23.5, -46.7)
        };
        var polygon = new Polygon(vertices);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => polygon.ToWKT());
    }

    [Fact]
    public void TestPolygon_FromWKT()
    {
        // Arrange
        var wkt = "POLYGON((-46.6 -23.5, -46.7 -23.5, -46.6 -23.6, -46.6 -23.5))";

        // Act
        var polygon = Polygon.FromWKT(wkt);

        // Assert
        Assert.NotNull(polygon);
        Assert.Equal(4, polygon.Vertices.Count);
        Assert.Equal(-23.5, polygon.Vertices[0].Latitude);
        Assert.Equal(-46.6, polygon.Vertices[0].Longitude);
    }

    [Fact]
    public void TestPolygon_FromWKT_CaseInsensitive()
    {
        // Arrange
        var wkt = "polygon((-46.6 -23.5, -46.7 -23.5, -46.6 -23.6, -46.6 -23.5))";

        // Act
        var polygon = Polygon.FromWKT(wkt);

        // Assert
        Assert.NotNull(polygon);
        Assert.Equal(4, polygon.Vertices.Count);
    }

    [Fact]
    public void TestPolygon_FromWKT_Null()
    {
        // Act
        var polygon = Polygon.FromWKT(null);

        // Assert
        Assert.Null(polygon);
    }

    [Fact]
    public void TestPolygon_FromWKT_Invalid()
    {
        // Arrange
        var wkt = "INVALID WKT STRING";

        // Act
        var polygon = Polygon.FromWKT(wkt);

        // Assert
        Assert.Null(polygon);
    }

    [Fact]
    public void TestPolygon_RoundTrip()
    {
        // Arrange
        var vertices = new List<Point>
        {
            new Point(-23.5, -46.6),
            new Point(-23.5, -46.7),
            new Point(-23.6, -46.6)
        };
        var originalPolygon = new Polygon(vertices);

        // Act
        var wkt = originalPolygon.ToWKT();
        var restoredPolygon = Polygon.FromWKT(wkt);

        // Assert
        // Original has 3 vertices, but ToWKT auto-closes it to 4 points in WKT
        // FromWKT reads all 4 points, so restored polygon has 4 vertices
        Assert.Equal(3, originalPolygon.Vertices.Count);
        Assert.Equal(4, restoredPolygon.Vertices.Count);
        
        // Verify the first 3 vertices match
        for (int i = 0; i < 3; i++)
        {
            Assert.Equal(originalPolygon.Vertices[i].Latitude, restoredPolygon.Vertices[i].Latitude);
            Assert.Equal(originalPolygon.Vertices[i].Longitude, restoredPolygon.Vertices[i].Longitude);
        }
        
        // Verify the 4th vertex is the same as the first (closed ring)
        Assert.Equal(restoredPolygon.Vertices[0].Latitude, restoredPolygon.Vertices[3].Latitude);
        Assert.Equal(restoredPolygon.Vertices[0].Longitude, restoredPolygon.Vertices[3].Longitude);
    }

    [Fact]
    public void TestInsertBuilder_ValueAsPolygon()
    {
        // Arrange
        var vertices = new List<Point>
        {
            new Point(-23.5, -46.6),
            new Point(-23.5, -46.7),
            new Point(-23.6, -46.6)
        };
        var polygon = new Polygon(vertices);

        var builder = new InsertQueryBuilder()
            .Table("zones")
            .Value("id", 1)
            .ValueAsPolygon("area", polygon);

        // Act
        var (sql, command) = builder.Build();

        // Assert
        Assert.Contains("INSERT INTO", sql);
        Assert.Contains("`zones`", sql);
        Assert.Contains("`area`", sql);
        Assert.Contains("ST_PolygonFromText(@p1, 4326)", sql);
        
        // Verify parameter is WKT string
        var param = command.Parameters["@p1"];
        Assert.NotNull(param);
        Assert.IsType<string>(param.Value);
        
        var wkt = (string)param.Value;
        Assert.StartsWith("POLYGON((", wkt);
    }

    [Fact]
    public void TestInsertBuilder_ValueAsPolygon_Null()
    {
        // Arrange
        var builder = new InsertQueryBuilder()
            .Table("zones")
            .Value("id", 1)
            .ValueAsPolygon("area", null);

        // Act
        var (sql, command) = builder.Build();

        // Assert
        var param = command.Parameters["@p1"];
        Assert.NotNull(param);
        Assert.Equal(DBNull.Value, param.Value);
    }

    [Fact]
    public void TestUpdateBuilder_SetAsPolygon()
    {
        // Arrange
        var vertices = new List<Point>
        {
            new Point(-22.9, -43.2),
            new Point(-22.9, -43.3),
            new Point(-23.0, -43.2)
        };
        var polygon = new Polygon(vertices);

        var builder = new UpdateQueryBuilder()
            .Table("zones")
            .SetAsPolygon("area", polygon)
            .Where("id", 1);

        // Act
        var (sql, command) = builder.Build();

        // Assert
        Assert.Contains("UPDATE", sql);
        Assert.Contains("`zones`", sql);
        Assert.Contains("SET `area` = ST_PolygonFromText(@p0, 4326)", sql);
        Assert.Contains("WHERE", sql);
        
        // Verify parameter is WKT string
        var param = command.Parameters["@p0"];
        Assert.NotNull(param);
        Assert.IsType<string>(param.Value);
    }

    [Fact]
    public void TestUpdateBuilder_SetAsPolygon_Null()
    {
        // Arrange
        var builder = new UpdateQueryBuilder()
            .Table("zones")
            .SetAsPolygon("area", null)
            .Where("id", 1);

        // Act
        var (sql, command) = builder.Build();

        // Assert
        var param = command.Parameters["@p0"];
        Assert.NotNull(param);
        Assert.Equal(DBNull.Value, param.Value);
    }

    [Fact]
    public void TestToModel_PolygonDeserialization()
    {
        // Arrange
        var wkt = "POLYGON((-46.6 -23.5, -46.7 -23.5, -46.6 -23.6, -46.6 -23.5))";
        
        var data = new List<Dictionary<string, object>>
        {
            new Dictionary<string, object>
            {
                { "Id", 1 },
                { "Name", "Downtown Zone" },
                { "Area", wkt }
            }
        };

        // Act
        using var reader = new MySQLReader(new FakeDataReader(data));
        reader.Read();
        var zone = reader.ToModel<ZoneModel>();

        // Assert
        Assert.Equal(1, zone.Id);
        Assert.Equal("Downtown Zone", zone.Name);
        Assert.NotNull(zone.Area);
        Assert.Equal(4, zone.Area.Vertices.Count);
        Assert.Equal(-23.5, zone.Area.Vertices[0].Latitude);
        Assert.Equal(-46.6, zone.Area.Vertices[0].Longitude);
    }

    [Fact]
    public void TestToModel_PolygonDeserialization_Null()
    {
        // Arrange
        var data = new List<Dictionary<string, object>>
        {
            new Dictionary<string, object>
            {
                { "Id", 1 },
                { "Name", "Test Zone" },
                { "Area", DBNull.Value }
            }
        };

        // Act
        using var reader = new MySQLReader(new FakeDataReader(data));
        reader.Read();
        var zone = reader.ToModel<ZoneModel>();

        // Assert
        Assert.Equal(1, zone.Id);
        Assert.Equal("Test Zone", zone.Name);
        Assert.Null(zone.Area);
    }

    [Fact]
    public void TestPolygon_Equality()
    {
        // Arrange
        var vertices1 = new List<Point>
        {
            new Point(-23.5, -46.6),
            new Point(-23.5, -46.7),
            new Point(-23.6, -46.6)
        };
        var vertices2 = new List<Point>
        {
            new Point(-23.5, -46.6),
            new Point(-23.5, -46.7),
            new Point(-23.6, -46.6)
        };
        var vertices3 = new List<Point>
        {
            new Point(-22.0, -45.0),
            new Point(-22.0, -45.1),
            new Point(-22.1, -45.0)
        };

        var polygon1 = new Polygon(vertices1);
        var polygon2 = new Polygon(vertices2);
        var polygon3 = new Polygon(vertices3);

        // Assert
        Assert.True(polygon1.Equals(polygon2));
        Assert.False(polygon1.Equals(polygon3));
        Assert.False(polygon1.Equals(null));
    }

    [Fact]
    public void TestPolygon_ToString()
    {
        // Arrange
        var vertices = new List<Point>
        {
            new Point(-23.5, -46.6),
            new Point(-23.5, -46.7),
            new Point(-23.6, -46.6)
        };
        var polygon = new Polygon(vertices);

        // Act
        var str = polygon.ToString();

        // Assert
        Assert.StartsWith("POLYGON((", str);
        Assert.EndsWith("))", str);
    }

    [Fact]
    public void TestInsertBuilder_MixedPolygonAndNormalValues()
    {
        // Arrange
        var vertices = new List<Point>
        {
            new Point(-23.5, -46.6),
            new Point(-23.5, -46.7),
            new Point(-23.6, -46.6)
        };
        var polygon = new Polygon(vertices);

        var builder = new InsertQueryBuilder()
            .Table("zones")
            .Value("id", 1)
            .Value("name", "Test Zone")
            .ValueAsPolygon("area", polygon)
            .Value("created_at", DateTime.Now);

        // Act
        var (sql, command) = builder.Build();

        // Assert
        Assert.Contains("INSERT INTO", sql);
        Assert.Contains("`id`", sql);
        Assert.Contains("`name`", sql);
        Assert.Contains("`area`", sql);
        Assert.Contains("`created_at`", sql);
        Assert.Contains("ST_PolygonFromText(@p2, 4326)", sql);
        
        // Verify we have 4 parameters
        Assert.Equal(4, command.Parameters.Count);
    }
}

// Test model
public class ZoneModel
{
    public int Id { get; set; }
    public string Name { get; set; }
    public Polygon Area { get; set; }
}
