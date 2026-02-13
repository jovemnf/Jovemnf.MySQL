using System;
using System.Collections.Generic;
using System.Linq;

namespace Jovemnf.MySQL.Geometry;

/// <summary>
/// Extension methods for spatial operations on Point and Polygon objects.
/// </summary>
public static class GeometryExtensions
{
    /// <summary>
    /// Calcula a distância em metros entre dois pontos usando a fórmula de Haversine.
    /// </summary>
    public static double DistanceTo(this Point from, Point to)
    {
        const double earthRadiusKm = 6371.0;

        var dLat = DegreesToRadians(to.Latitude - from.Latitude);
        var dLon = DegreesToRadians(to.Longitude - from.Longitude);

        var lat1 = DegreesToRadians(from.Latitude);
        var lat2 = DegreesToRadians(to.Latitude);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2) * Math.Cos(lat1) * Math.Cos(lat2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return earthRadiusKm * c * 1000; // Convert to meters
    }

    /// <summary>
    /// Verifica se um ponto está dentro de um polígono usando o algoritmo Ray Casting.
    /// </summary>
    public static bool IsInside(this Point point, Polygon polygon)
    {
        if (polygon == null || polygon.Vertices == null || polygon.Vertices.Count < 3)
            return false;

        bool inside = false;
        int j = polygon.Vertices.Count - 1;

        for (int i = 0; i < polygon.Vertices.Count; i++)
        {
            var vi = polygon.Vertices[i];
            var vj = polygon.Vertices[j];

            if ((vi.Latitude > point.Latitude) != (vj.Latitude > point.Latitude) &&
                (point.Longitude < (vj.Longitude - vi.Longitude) * (point.Latitude - vi.Latitude) / 
                (vj.Latitude - vi.Latitude) + vi.Longitude))
            {
                inside = !inside;
            }
            j = i;
        }

        return inside;
    }

    /// <summary>
    /// Verifica se um polígono contém um ponto.
    /// </summary>
    public static bool Contains(this Polygon polygon, Point point)
    {
        return point.IsInside(polygon);
    }

    /// <summary>
    /// Filtra uma lista de pontos que estão dentro do polígono.
    /// </summary>
    public static List<Point> PointsInside(this Polygon polygon, IEnumerable<Point> points)
    {
        return points.Where(p => p.IsInside(polygon)).ToList();
    }

    /// <summary>
    /// Filtra uma lista de pontos que estão dentro de um raio (em metros) de um ponto central.
    /// </summary>
    public static List<Point> PointsWithinRadius(this Point center, IEnumerable<Point> points, double radiusMeters)
    {
        return points.Where(p => center.DistanceTo(p) <= radiusMeters).ToList();
    }

    /// <summary>
    /// Encontra o ponto mais próximo de um ponto de referência.
    /// </summary>
    public static Point FindNearest(this Point reference, IEnumerable<Point> points)
    {
        Point nearest = null;
        double minDistance = double.MaxValue;

        foreach (var point in points)
        {
            var distance = reference.DistanceTo(point);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = point;
            }
        }

        return nearest;
    }

    /// <summary>
    /// Encontra os N pontos mais próximos de um ponto de referência.
    /// </summary>
    public static List<Point> FindNearestN(this Point reference, IEnumerable<Point> points, int count)
    {
        return points
            .OrderBy(p => reference.DistanceTo(p))
            .Take(count)
            .ToList();
    }

    /// <summary>
    /// Calcula o centro (centroide) de um polígono.
    /// </summary>
    public static Point GetCenter(this Polygon polygon)
    {
        if (polygon == null || polygon.Vertices == null || polygon.Vertices.Count == 0)
            return null;

        double sumLat = 0;
        double sumLng = 0;
        int count = polygon.Vertices.Count;

        foreach (var vertex in polygon.Vertices)
        {
            sumLat += vertex.Latitude;
            sumLng += vertex.Longitude;
        }

        return new Point(sumLat / count, sumLng / count, polygon.SRID);
    }

    /// <summary>
    /// Calcula a área aproximada de um polígono em metros quadrados.
    /// Usa a fórmula de Shoelace (para polígonos pequenos).
    /// </summary>
    public static double GetArea(this Polygon polygon)
    {
        if (polygon == null || polygon.Vertices == null || polygon.Vertices.Count < 3)
            return 0;

        const double earthRadiusMeters = 6371000.0;
        
        double area = 0;
        int j = polygon.Vertices.Count - 1;

        for (int i = 0; i < polygon.Vertices.Count; i++)
        {
            var vi = polygon.Vertices[i];
            var vj = polygon.Vertices[j];
            
            area += (DegreesToRadians(vj.Longitude) - DegreesToRadians(vi.Longitude)) * 
                    (2 + Math.Sin(DegreesToRadians(vi.Latitude)) + Math.Sin(DegreesToRadians(vj.Latitude)));
            j = i;
        }

        area = Math.Abs(area * earthRadiusMeters * earthRadiusMeters / 2.0);
        return area;
    }

    /// <summary>
    /// Verifica se dois polígonos se sobrepõem (overlap).
    /// Nota: Esta é uma verificação simplificada que verifica se algum vértice de um está dentro do outro.
    /// </summary>
    public static bool Overlaps(this Polygon polygon1, Polygon polygon2)
    {
        if (polygon1 == null || polygon2 == null)
            return false;

        // Check if any vertex of polygon1 is inside polygon2
        foreach (var vertex in polygon1.Vertices)
        {
            if (vertex.IsInside(polygon2))
                return true;
        }

        // Check if any vertex of polygon2 is inside polygon1
        foreach (var vertex in polygon2.Vertices)
        {
            if (vertex.IsInside(polygon1))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Cria um círculo (polígono aproximado) ao redor de um ponto.
    /// </summary>
    /// <param name="center">Ponto central</param>
    /// <param name="radiusMeters">Raio em metros</param>
    /// <param name="segments">Número de segmentos (quanto maior, mais circular)</param>
    public static Polygon CreateCircle(this Point center, double radiusMeters, int segments = 32)
    {
        const double earthRadiusMeters = 6371000.0;
        var vertices = new List<Point>();

        for (int i = 0; i < segments; i++)
        {
            double angle = 2 * Math.PI * i / segments;
            
            // Calculate offset in degrees
            double latOffset = (radiusMeters / earthRadiusMeters) * (180 / Math.PI);
            double lngOffset = (radiusMeters / (earthRadiusMeters * Math.Cos(DegreesToRadians(center.Latitude)))) * (180 / Math.PI);
            
            double lat = center.Latitude + latOffset * Math.Sin(angle);
            double lng = center.Longitude + lngOffset * Math.Cos(angle);
            
            vertices.Add(new Point(lat, lng, center.SRID));
        }

        return new Polygon(vertices, center.SRID);
    }

    /// <summary>
    /// Cria um retângulo (bounding box) ao redor de um ponto.
    /// </summary>
    public static Polygon CreateBoundingBox(this Point center, double widthMeters, double heightMeters)
    {
        const double earthRadiusMeters = 6371000.0;
        
        double latOffset = (heightMeters / 2 / earthRadiusMeters) * (180 / Math.PI);
        double lngOffset = (widthMeters / 2 / (earthRadiusMeters * Math.Cos(DegreesToRadians(center.Latitude)))) * (180 / Math.PI);
        
        var vertices = new List<Point>
        {
            new Point(center.Latitude - latOffset, center.Longitude - lngOffset, center.SRID), // SW
            new Point(center.Latitude - latOffset, center.Longitude + lngOffset, center.SRID), // SE
            new Point(center.Latitude + latOffset, center.Longitude + lngOffset, center.SRID), // NE
            new Point(center.Latitude + latOffset, center.Longitude - lngOffset, center.SRID)  // NW
        };

        return new Polygon(vertices, center.SRID);
    }

    private static double DegreesToRadians(double degrees)
    {
        return degrees * Math.PI / 180.0;
    }

    private static double RadiansToDegrees(double radians)
    {
        return radians * 180.0 / Math.PI;
    }
}
