using System;

namespace Jovemnf.MySQL.Geometry;

/// <summary>
/// Represents a geographic point with latitude and longitude (SRID 4326 - WGS84).
/// </summary>
public class Point
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int SRID { get; set; } = 4326;

    public Point() { }

    public Point(double latitude, double longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
    }

    public Point(double latitude, double longitude, int srid)
    {
        Latitude = latitude;
        Longitude = longitude;
        SRID = srid;
    }

    /// <summary>
    /// Converts the Point to WKB (Well-Known Binary) format for MySQL.
    /// Format: [SRID 4 bytes] [byte order 1 byte] [type 4 bytes] [X 8 bytes] [Y 8 bytes]
    /// </summary>
    public byte[] ToWKB()
    {
        var wkb = new byte[25]; // Total: 4 + 1 + 4 + 8 + 8 = 25 bytes
        
        // SRID (4 bytes, little-endian)
        BitConverter.GetBytes(SRID).CopyTo(wkb, 0);
        
        // Byte order (1 = little-endian)
        wkb[4] = 1;
        
        // Geometry type (1 = POINT)
        BitConverter.GetBytes(1).CopyTo(wkb, 5);
        
        // X coordinate (Longitude) - MySQL uses Longitude, Latitude order
        BitConverter.GetBytes(Longitude).CopyTo(wkb, 9);
        
        // Y coordinate (Latitude)
        BitConverter.GetBytes(Latitude).CopyTo(wkb, 17);
        
        return wkb;
    }

    /// <summary>
    /// Creates a Point from WKB (Well-Known Binary) format.
    /// </summary>
    public static Point FromWKB(byte[] wkb)
    {
        if (wkb == null || wkb.Length < 25)
            return null;
        
        // Read SRID (first 4 bytes)
        int srid = BitConverter.ToInt32(wkb, 0);
        
        // Read X (Longitude) - bytes 9-16
        double longitude = BitConverter.ToDouble(wkb, 9);
        
        // Read Y (Latitude) - bytes 17-24
        double latitude = BitConverter.ToDouble(wkb, 17);
        
        return new Point(latitude, longitude, srid);
    }

    public override string ToString()
    {
        var lat = Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var lng = Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return $"POINT({lat}, {lng})";
    }

    public override bool Equals(object obj)
    {
        if (obj is Point other)
        {
            return Math.Abs(Latitude - other.Latitude) < 0.0000001 
                && Math.Abs(Longitude - other.Longitude) < 0.0000001
                && SRID == other.SRID;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Latitude, Longitude, SRID);
    }
}
