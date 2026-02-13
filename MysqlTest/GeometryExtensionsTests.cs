using System;
using System.Collections.Generic;
using Jovemnf.MySQL;
using Jovemnf.MySQL.Geometry;
using Xunit;

namespace MysqlTest;

public class GeometryExtensionsTests
{
    [Fact]
    public void TestDistanceTo_SamePoint()
    {
        // Arrange
        var point1 = new Point(-23.551, -46.633);
        var point2 = new Point(-23.551, -46.633);

        // Act
        var distance = point1.DistanceTo(point2);

        // Assert
        Assert.True(distance < 1); // Less than 1 meter
    }

    [Fact]
    public void TestDistanceTo_KnownDistance()
    {
        // Arrange - São Paulo to Rio de Janeiro (approximately 360km)
        var saoPaulo = new Point(-23.5505, -46.6333);
        var rio = new Point(-22.9068, -43.1729);

        // Act
        var distance = saoPaulo.DistanceTo(rio);

        // Assert - Should be around 360,000 meters (360km)
        Assert.True(distance > 350000 && distance < 370000);
    }

    [Fact]
    public void TestIsInside_PointInsidePolygon()
    {
        // Arrange - Square polygon
        var polygon = new Polygon(new List<Point>
        {
            new Point(-23.5, -46.6),
            new Point(-23.5, -46.7),
            new Point(-23.6, -46.7),
            new Point(-23.6, -46.6)
        });

        var pointInside = new Point(-23.55, -46.65);

        // Act
        var isInside = pointInside.IsInside(polygon);

        // Assert
        Assert.True(isInside);
    }

    [Fact]
    public void TestIsInside_PointOutsidePolygon()
    {
        // Arrange
        var polygon = new Polygon(new List<Point>
        {
            new Point(-23.5, -46.6),
            new Point(-23.5, -46.7),
            new Point(-23.6, -46.7),
            new Point(-23.6, -46.6)
        });

        var pointOutside = new Point(-23.7, -46.8);

        // Act
        var isInside = pointOutside.IsInside(polygon);

        // Assert
        Assert.False(isInside);
    }

    [Fact]
    public void TestContains_PolygonContainsPoint()
    {
        // Arrange
        var polygon = new Polygon(new List<Point>
        {
            new Point(-23.5, -46.6),
            new Point(-23.5, -46.7),
            new Point(-23.6, -46.7),
            new Point(-23.6, -46.6)
        });

        var point = new Point(-23.55, -46.65);

        // Act
        var contains = polygon.Contains(point);

        // Assert
        Assert.True(contains);
    }

    [Fact]
    public void TestPointsInside_FiltersCorrectly()
    {
        // Arrange
        var polygon = new Polygon(new List<Point>
        {
            new Point(-23.5, -46.6),
            new Point(-23.5, -46.7),
            new Point(-23.6, -46.7),
            new Point(-23.6, -46.6)
        });

        var points = new List<Point>
        {
            new Point(-23.55, -46.65),  // Inside
            new Point(-23.7, -46.8),    // Outside
            new Point(-23.52, -46.62),  // Inside
            new Point(-23.4, -46.5)     // Outside
        };

        // Act
        var insidePoints = polygon.PointsInside(points);

        // Assert
        Assert.Equal(2, insidePoints.Count);
    }

    [Fact]
    public void TestPointsWithinRadius_FiltersCorrectly()
    {
        // Arrange
        var center = new Point(-23.551, -46.633);
        var points = new List<Point>
        {
            new Point(-23.551, -46.633),  // Same point (0m)
            new Point(-23.552, -46.634),  // ~150m
            new Point(-23.560, -46.640),  // ~1000m
            new Point(-23.600, -46.700)   // ~10000m
        };

        // Act
        var nearbyPoints = center.PointsWithinRadius(points, 500);

        // Assert
        Assert.Equal(2, nearbyPoints.Count);
    }

    [Fact]
    public void TestFindNearest_ReturnsClosestPoint()
    {
        // Arrange
        var reference = new Point(-23.551, -46.633);
        var points = new List<Point>
        {
            new Point(-23.560, -46.640),  // Far
            new Point(-23.552, -46.634),  // Close
            new Point(-23.600, -46.700)   // Very far
        };

        // Act
        var nearest = reference.FindNearest(points);

        // Assert
        Assert.Equal(-23.552, nearest.Latitude);
        Assert.Equal(-46.634, nearest.Longitude);
    }

    [Fact]
    public void TestFindNearestN_ReturnsTopN()
    {
        // Arrange
        var reference = new Point(-23.551, -46.633);
        var points = new List<Point>
        {
            new Point(-23.560, -46.640),  // Medium
            new Point(-23.552, -46.634),  // Closest
            new Point(-23.600, -46.700),  // Farthest
            new Point(-23.555, -46.636)   // Second closest
        };

        // Act
        var nearest2 = reference.FindNearestN(points, 2);

        // Assert
        Assert.Equal(2, nearest2.Count);
        Assert.Equal(-23.552, nearest2[0].Latitude); // Closest
        Assert.Equal(-23.555, nearest2[1].Latitude); // Second closest
    }

    [Fact]
    public void TestGetCenter_CalculatesCentroid()
    {
        // Arrange - Square polygon
        var polygon = new Polygon(new List<Point>
        {
            new Point(-23.5, -46.6),
            new Point(-23.5, -46.7),
            new Point(-23.6, -46.7),
            new Point(-23.6, -46.6)
        });

        // Act
        var center = polygon.GetCenter();

        // Assert - Center should be at (-23.55, -46.65)
        Assert.True(Math.Abs(center.Latitude - (-23.55)) < 0.01);
        Assert.True(Math.Abs(center.Longitude - (-46.65)) < 0.01);
    }

    [Fact]
    public void TestGetArea_CalculatesApproximateArea()
    {
        // Arrange - Small square (approximately 11km x 11km)
        var polygon = new Polygon(new List<Point>
        {
            new Point(-23.5, -46.6),
            new Point(-23.5, -46.7),
            new Point(-23.6, -46.7),
            new Point(-23.6, -46.6)
        });

        // Act
        var area = polygon.GetArea();

        // Assert - Should be around 120,000,000 m² (120 km²)
        Assert.True(area > 100000000 && area < 140000000);
    }

    [Fact]
    public void TestOverlaps_DetectsOverlap()
    {
        // Arrange - Two overlapping squares
        var polygon1 = new Polygon(new List<Point>
        {
            new Point(-23.5, -46.6),
            new Point(-23.5, -46.7),
            new Point(-23.6, -46.7),
            new Point(-23.6, -46.6)
        });

        var polygon2 = new Polygon(new List<Point>
        {
            new Point(-23.55, -46.65),
            new Point(-23.55, -46.75),
            new Point(-23.65, -46.75),
            new Point(-23.65, -46.65)
        });

        // Act
        var overlaps = polygon1.Overlaps(polygon2);

        // Assert
        Assert.True(overlaps);
    }

    [Fact]
    public void TestOverlaps_NoOverlap()
    {
        // Arrange - Two separate squares
        var polygon1 = new Polygon(new List<Point>
        {
            new Point(-23.5, -46.6),
            new Point(-23.5, -46.7),
            new Point(-23.6, -46.7),
            new Point(-23.6, -46.6)
        });

        var polygon2 = new Polygon(new List<Point>
        {
            new Point(-23.8, -46.8),
            new Point(-23.8, -46.9),
            new Point(-23.9, -46.9),
            new Point(-23.9, -46.8)
        });

        // Act
        var overlaps = polygon1.Overlaps(polygon2);

        // Assert
        Assert.False(overlaps);
    }

    [Fact]
    public void TestCreateCircle_CreatesPolygon()
    {
        // Arrange
        var center = new Point(-23.551, -46.633);
        var radiusMeters = 1000;

        // Act
        var circle = center.CreateCircle(radiusMeters, 16);

        // Assert
        Assert.Equal(16, circle.Vertices.Count);
        
        // Verify all points are approximately at the radius distance
        foreach (var vertex in circle.Vertices)
        {
            var distance = center.DistanceTo(vertex);
            Assert.True(Math.Abs(distance - radiusMeters) < 100); // Within 100m tolerance
        }
    }

    [Fact]
    public void TestCreateBoundingBox_CreatesRectangle()
    {
        // Arrange
        var center = new Point(-23.551, -46.633);
        var width = 1000;
        var height = 2000;

        // Act
        var box = center.CreateBoundingBox(width, height);

        // Assert
        Assert.Equal(4, box.Vertices.Count);
        
        // Verify it's centered
        var boxCenter = box.GetCenter();
        Assert.True(Math.Abs(boxCenter.Latitude - center.Latitude) < 0.001);
        Assert.True(Math.Abs(boxCenter.Longitude - center.Longitude) < 0.001);
    }

    [Fact]
    public void TestIsInside_NullPolygon()
    {
        // Arrange
        var point = new Point(-23.551, -46.633);

        // Act
        var isInside = point.IsInside(null);

        // Assert
        Assert.False(isInside);
    }

    [Fact]
    public void TestGetCenter_NullPolygon()
    {
        // Arrange
        Polygon polygon = null;

        // Act
        var center = polygon.GetCenter();

        // Assert
        Assert.Null(center);
    }

    [Fact]
    public void TestFindNearest_EmptyList()
    {
        // Arrange
        var reference = new Point(-23.551, -46.633);
        var points = new List<Point>();

        // Act
        var nearest = reference.FindNearest(points);

        // Assert
        Assert.Null(nearest);
    }
}
