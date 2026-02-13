using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Jovemnf.MySQL.Geometry;

/// <summary>
/// Represents a geographic polygon with SRID 4326 (WGS84).
/// </summary>
public class Polygon
{
    public List<Point> Vertices { get; set; }
    public int SRID { get; set; } = 4326;

    public Polygon()
    {
        Vertices = new List<Point>();
    }

    public Polygon(List<Point> vertices)
    {
        Vertices = vertices ?? new List<Point>();
    }

    public Polygon(List<Point> vertices, int srid)
    {
        Vertices = vertices ?? new List<Point>();
        SRID = srid;
    }

    /// <summary>
    /// Converts the Polygon to WKT (Well-Known Text) format for MySQL.
    /// Format: POLYGON((lng1 lat1, lng2 lat2, ..., lng1 lat1))
    /// </summary>
    public string ToWKT()
    {
        if (Vertices == null || Vertices.Count < 3)
            throw new InvalidOperationException("Polygon must have at least 3 vertices");

        var points = new List<string>();
        foreach (var vertex in Vertices)
        {
            // MySQL uses Longitude, Latitude order (X, Y)
            // Use InvariantCulture to ensure dot as decimal separator
            var lng = vertex.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var lat = vertex.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
            points.Add($"{lng} {lat}");
        }

        // Close the ring if not already closed
        var first = Vertices[0];
        var last = Vertices[Vertices.Count - 1];
        if (!first.Equals(last))
        {
            var lng = first.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var lat = first.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
            points.Add($"{lng} {lat}");
        }

        return $"POLYGON(({string.Join(", ", points)}))";
    }

    /// <summary>
    /// Creates a Polygon from WKT (Well-Known Text) format.
    /// </summary>
    public static Polygon FromWKT(string wkt)
    {
        if (string.IsNullOrWhiteSpace(wkt))
            return null;

        // Parse: POLYGON((x1 y1, x2 y2, ...))
        var match = Regex.Match(
            wkt, 
            @"POLYGON\s*\(\s*\((.*?)\)\s*\)", 
            RegexOptions.IgnoreCase
        );

        if (!match.Success)
            return null;

        var coordsStr = match.Groups[1].Value;
        var coordPairs = coordsStr.Split(',');
        
        var vertices = new List<Point>();
        foreach (var pair in coordPairs)
        {
            var coords = pair.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (coords.Length >= 2)
            {
                if (double.TryParse(coords[0], System.Globalization.NumberStyles.Float, 
                    System.Globalization.CultureInfo.InvariantCulture, out double lng) && 
                    double.TryParse(coords[1], System.Globalization.NumberStyles.Float, 
                    System.Globalization.CultureInfo.InvariantCulture, out double lat))
                {
                    vertices.Add(new Point(lat, lng));
                }
            }
        }

        return vertices.Count >= 3 ? new Polygon(vertices) : null;
    }

    /// <summary>
    /// Creates a Polygon from WKB (Well-Known Binary) format.
    /// Note: For simplicity, use ST_AsText in SELECT queries instead of ST_AsBinary.
    /// </summary>
    public static Polygon FromWKB(byte[] wkb)
    {
        // WKB parsing for polygons is complex. 
        // Recommend using ST_AsText in queries instead.
        throw new NotImplementedException("Use ST_AsText(polygon_column) in SELECT queries instead of ST_AsBinary");
    }

    public override string ToString()
    {
        return ToWKT();
    }

    public override bool Equals(object obj)
    {
        if (obj is Polygon other)
        {
            if (Vertices.Count != other.Vertices.Count || SRID != other.SRID)
                return false;

            for (int i = 0; i < Vertices.Count; i++)
            {
                if (!Vertices[i].Equals(other.Vertices[i]))
                    return false;
            }
            return true;
        }
        return false;
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(SRID);
        foreach (var vertex in Vertices)
        {
            hash.Add(vertex);
        }
        return hash.ToHashCode();
    }
}
