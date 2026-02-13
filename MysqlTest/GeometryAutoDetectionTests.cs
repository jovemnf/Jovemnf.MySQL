using System.Collections.Generic;
using Jovemnf.MySQL;
using Jovemnf.MySQL.Geometry;
using Jovemnf.MySQL.Builder;
using Xunit;

namespace MysqlTest;

/// <summary>
/// Tests for automatic GEOMETRY type detection in Value() and Set() methods
/// </summary>
public class GeometryAutoDetectionTests
{
    [Fact]
    public void TestInsertBuilder_Value_AutoDetectsPoint()
    {
        // Arrange
        var point = new Point(-23.551, -46.633);
        
        var builder = new InsertQueryBuilder()
            .Table("locations")
            .Value("id", 1)
            .Value("coordinates", point);  // ← Auto-detection!

        // Act
        var (sql, command) = builder.Build();

        // Assert
        Assert.Contains("ST_GeomFromWKB(@p1, 4326)", sql);
        
        var param = command.Parameters["@p1"];
        Assert.NotNull(param);
        Assert.IsType<byte[]>(param.Value);
    }

    [Fact]
    public void TestInsertBuilder_Value_AutoDetectsPolygon()
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
            .Value("area", polygon);  // ← Auto-detection!

        // Act
        var (sql, command) = builder.Build();

        // Assert
        Assert.Contains("ST_PolygonFromText(@p1, 4326)", sql);
        
        var param = command.Parameters["@p1"];
        Assert.NotNull(param);
        Assert.IsType<string>(param.Value);
    }

    [Fact]
    public void TestUpdateBuilder_Set_AutoDetectsPoint()
    {
        // Arrange
        var point = new Point(-22.9068, -43.1729);
        
        var builder = new UpdateQueryBuilder()
            .Table("locations")
            .Set("coordinates", point)  // ← Auto-detection!
            .Where("id", 1);

        // Act
        var (sql, command) = builder.Build();

        // Assert
        Assert.Contains("SET `coordinates` = ST_GeomFromWKB(@p0, 4326)", sql);
        
        var param = command.Parameters["@p0"];
        Assert.NotNull(param);
        Assert.IsType<byte[]>(param.Value);
    }

    [Fact]
    public void TestUpdateBuilder_Set_AutoDetectsPolygon()
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
            .Set("area", polygon)  // ← Auto-detection!
            .Where("id", 1);

        // Act
        var (sql, command) = builder.Build();

        // Assert
        Assert.Contains("SET `area` = ST_PolygonFromText(@p0, 4326)", sql);
        
        var param = command.Parameters["@p0"];
        Assert.NotNull(param);
        Assert.IsType<string>(param.Value);
    }

    [Fact]
    public void TestInsertBuilder_Value_MixedAutoDetection()
    {
        // Arrange
        var point = new Point(-23.551, -46.633);
        var vertices = new List<Point>
        {
            new Point(-23.5, -46.6),
            new Point(-23.5, -46.7),
            new Point(-23.6, -46.6)
        };
        var polygon = new Polygon(vertices);
        
        var builder = new InsertQueryBuilder()
            .Table("test")
            .Value("id", 1)
            .Value("name", "Test")
            .Value("location", point)    // ← Auto-detected as POINT
            .Value("zone", polygon);      // ← Auto-detected as POLYGON

        // Act
        var (sql, command) = builder.Build();

        // Assert
        Assert.Contains("ST_GeomFromWKB(@p2, 4326)", sql);
        Assert.Contains("ST_PolygonFromText(@p3, 4326)", sql);
        Assert.Equal(4, command.Parameters.Count);
    }

    [Fact]
    public void TestUpdateBuilder_Set_MixedAutoDetection()
    {
        // Arrange
        var point = new Point(-23.551, -46.633);
        var vertices = new List<Point>
        {
            new Point(-23.5, -46.6),
            new Point(-23.5, -46.7),
            new Point(-23.6, -46.6)
        };
        var polygon = new Polygon(vertices);
        
        var builder = new UpdateQueryBuilder()
            .Table("test")
            .Set("name", "Updated")
            .Set("location", point)       // ← Auto-detected as POINT
            .Set("zone", polygon)         // ← Auto-detected as POLYGON
            .Where("id", 1);

        // Act
        var (sql, command) = builder.Build();

        // Assert
        Assert.Contains("SET `name` = @p0", sql);
        Assert.Contains("`location` = ST_GeomFromWKB(@p1, 4326)", sql);
        Assert.Contains("`zone` = ST_PolygonFromText(@p2, 4326)", sql);
    }

    [Fact]
    public void TestInsertBuilder_ValueAsPoint_StillWorks()
    {
        // Arrange - Explicit method should still work
        var point = new Point(-23.551, -46.633);
        
        var builder = new InsertQueryBuilder()
            .Table("locations")
            .ValueAsPoint("coordinates", point);

        // Act
        var (sql, command) = builder.Build();

        // Assert
        Assert.Contains("ST_GeomFromWKB(@p0, 4326)", sql);
    }

    [Fact]
    public void TestUpdateBuilder_SetAsPolygon_StillWorks()
    {
        // Arrange - Explicit method should still work
        var vertices = new List<Point>
        {
            new Point(-23.5, -46.6),
            new Point(-23.5, -46.7),
            new Point(-23.6, -46.6)
        };
        var polygon = new Polygon(vertices);
        
        var builder = new UpdateQueryBuilder()
            .Table("zones")
            .SetAsPolygon("area", polygon)
            .Where("id", 1);

        // Act
        var (sql, command) = builder.Build();

        // Assert
        Assert.Contains("ST_PolygonFromText(@p0, 4326)", sql);
    }
}
