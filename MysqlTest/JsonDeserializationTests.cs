using System;
using System.Collections.Generic;
using Jovemnf.MySQL;
using Jovemnf.DateTimeStamp;
using MysqlTest.Fakes;
using Xunit;

namespace MysqlTest;

// Test models for JSON deserialization
public class Payload
{
    public double Lat { get; set; }
    public double Lng { get; set; }
}

public class Telemetria
{
    public int Id { get; set; }
    public Payload Payload { get; set; }
}

public class Address
{
    public string Street { get; set; }
    public string City { get; set; }
    public string ZipCode { get; set; }
}

public class Person
{
    public int Id { get; set; }
    public string Name { get; set; }
    public Address Address { get; set; }
}

public class OrderItem
{
    public string ProductName { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}

public class Order
{
    public int Id { get; set; }
    public List<OrderItem> Items { get; set; }
}

public class EventModel
{
    public int Id { get; set; }
    public MyDate EventDate { get; set; }
    public MyDateTime CreatedAt { get; set; }
}

public class MixedModel
{
    public int Id { get; set; }
    public string Name { get; set; }
    public Payload Location { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class JsonDeserializationTests
{
    [Fact]
    public void TestToModel_SimpleJsonObject()
    {
        // Arrange: Simple JSON object with lat/lng
        var data = new List<Dictionary<string, object>>
        {
            new Dictionary<string, object>
            {
                { "Id", 1 },
                { "Payload", "{\"lat\": -23.551, \"lng\": -46.633}" }
            }
        };

        // Act
        using var reader = new MySQLReader(new FakeDataReader(data));
        reader.Read();
        var model = reader.ToModel<Telemetria>();

        // Assert
        Assert.Equal(1, model.Id);
        Assert.NotNull(model.Payload);
        Assert.Equal(-23.551, model.Payload.Lat);
        Assert.Equal(-46.633, model.Payload.Lng);
    }

    [Fact]
    public void TestToModel_JsonWithExtraProperties()
    {
        // Arrange: User's exact use case - JSON with many properties, but we only want lat/lng
        var data = new List<Dictionary<string, object>>
        {
            new Dictionary<string, object>
            {
                { "Id", 1 },
                { "Payload", "{\"ign\": true, \"lat\": -23.551, \"lng\": -46.633, \"sat\": 12, \"attrs\": {\"battery_v\": 12.4, \"odometer_km\": 12345.6}, \"speed\": 58}" }
            }
        };

        // Act
        using var reader = new MySQLReader(new FakeDataReader(data));
        reader.Read();
        var model = reader.ToModel<Telemetria>();

        // Assert
        Assert.Equal(1, model.Id);
        Assert.NotNull(model.Payload);
        Assert.Equal(-23.551, model.Payload.Lat);
        Assert.Equal(-46.633, model.Payload.Lng);
        // Extra properties (ign, sat, attrs, speed) are ignored - this is the key test!
    }

    [Fact]
    public void TestToModel_CaseInsensitiveJsonMapping()
    {
        // Arrange: JSON with lowercase properties, C# class with PascalCase
        var data = new List<Dictionary<string, object>>
        {
            new Dictionary<string, object>
            {
                { "Id", 2 },
                { "Payload", "{\"lat\": -22.123, \"lng\": -45.456}" }  // lowercase in JSON
            }
        };

        // Act
        using var reader = new MySQLReader(new FakeDataReader(data));
        reader.Read();
        var model = reader.ToModel<Telemetria>();

        // Assert - Should map despite case difference
        Assert.Equal(2, model.Id);
        Assert.NotNull(model.Payload);
        Assert.Equal(-22.123, model.Payload.Lat);  // Lat in C# class
        Assert.Equal(-45.456, model.Payload.Lng);  // Lng in C# class
    }

    [Fact]
    public void TestToModel_NestedJsonObject()
    {
        // Arrange: JSON with nested object
        var data = new List<Dictionary<string, object>>
        {
            new Dictionary<string, object>
            {
                { "Id", 10 },
                { "Name", "John Doe" },
                { "Address", "{\"street\": \"123 Main St\", \"city\": \"São Paulo\", \"zipCode\": \"01234-567\"}" }
            }
        };

        // Act
        using var reader = new MySQLReader(new FakeDataReader(data));
        reader.Read();
        var model = reader.ToModel<Person>();

        // Assert
        Assert.Equal(10, model.Id);
        Assert.Equal("John Doe", model.Name);
        Assert.NotNull(model.Address);
        Assert.Equal("123 Main St", model.Address.Street);
        Assert.Equal("São Paulo", model.Address.City);
        Assert.Equal("01234-567", model.Address.ZipCode);
    }

    [Fact]
    public void TestToModel_JsonArray()
    {
        // Arrange: JSON array of objects
        var data = new List<Dictionary<string, object>>
        {
            new Dictionary<string, object>
            {
                { "Id", 100 },
                { "Items", "[{\"productName\": \"Widget\", \"quantity\": 5, \"price\": 19.99}, {\"productName\": \"Gadget\", \"quantity\": 3, \"price\": 29.99}]" }
            }
        };

        // Act
        using var reader = new MySQLReader(new FakeDataReader(data));
        reader.Read();
        var model = reader.ToModel<Order>();

        // Assert
        Assert.Equal(100, model.Id);
        Assert.NotNull(model.Items);
        Assert.Equal(2, model.Items.Count);
        
        Assert.Equal("Widget", model.Items[0].ProductName);
        Assert.Equal(5, model.Items[0].Quantity);
        Assert.Equal(19.99m, model.Items[0].Price);
        
        Assert.Equal("Gadget", model.Items[1].ProductName);
        Assert.Equal(3, model.Items[1].Quantity);
        Assert.Equal(29.99m, model.Items[1].Price);
    }

    [Fact]
    public void TestToModel_MyDateFromDateTime()
    {
        // Arrange: DateTime values that will be converted to MyDate/MyDateTime
        var data = new List<Dictionary<string, object>>
        {
            new Dictionary<string, object>
            {
                { "Id", 5 },
                { "EventDate", new DateTime(2024, 12, 25, 0, 0, 0) },
                { "CreatedAt", new DateTime(2024, 12, 25, 14, 30, 0) }
            }
        };

        // Act
        using var reader = new MySQLReader(new FakeDataReader(data));
        reader.Read();
        var model = reader.ToModel<EventModel>();

        // Assert
        Assert.Equal(5, model.Id);
        Assert.NotNull(model.EventDate);
        Assert.NotNull(model.CreatedAt);
        // MyDate and MyDateTime should be properly converted from DateTime
    }

    [Fact]
    public void TestToModel_InvalidJson()
    {
        // Arrange: Invalid JSON should not crash, just skip the property
        var data = new List<Dictionary<string, object>>
        {
            new Dictionary<string, object>
            {
                { "Id", 99 },
                { "Payload", "{invalid json here}" }  // Invalid JSON
            }
        };

        // Act
        using var reader = new MySQLReader(new FakeDataReader(data));
        reader.Read();
        var model = reader.ToModel<Telemetria>();

        // Assert - Should not crash, Payload should be null or default
        Assert.Equal(99, model.Id);
        // Payload might be null or have default values, but shouldn't crash
    }

    [Fact]
    public void TestToModel_NullJsonProperty()
    {
        // Arrange: Null JSON value
        var data = new List<Dictionary<string, object>>
        {
            new Dictionary<string, object>
            {
                { "Id", 7 },
                { "Payload", DBNull.Value }  // NULL in database
            }
        };

        // Act
        using var reader = new MySQLReader(new FakeDataReader(data));
        reader.Read();
        var model = reader.ToModel<Telemetria>();

        // Assert
        Assert.Equal(7, model.Id);
        Assert.Null(model.Payload);  // Should handle NULL gracefully
    }

    [Fact]
    public void TestToModel_MixedJsonAndNormalColumns()
    {
        // Arrange: Mix of JSON and normal columns
        var data = new List<Dictionary<string, object>>
        {
            new Dictionary<string, object>
            {
                { "Id", 50 },
                { "Name", "Test Location" },
                { "Location", "{\"lat\": -23.5, \"lng\": -46.6}" },  // JSON column
                { "CreatedAt", new DateTime(2024, 1, 15, 10, 30, 0) }  // Normal DateTime column
            }
        };

        // Act
        using var reader = new MySQLReader(new FakeDataReader(data));
        reader.Read();
        var model = reader.ToModel<MixedModel>();

        // Assert
        Assert.Equal(50, model.Id);
        Assert.Equal("Test Location", model.Name);
        Assert.NotNull(model.Location);
        Assert.Equal(-23.5, model.Location.Lat);
        Assert.Equal(-46.6, model.Location.Lng);
        Assert.Equal(new DateTime(2024, 1, 15, 10, 30, 0), model.CreatedAt);
    }

    [Fact]
    public void TestToModel_EmptyJsonObject()
    {
        // Arrange: Empty JSON object
        var data = new List<Dictionary<string, object>>
        {
            new Dictionary<string, object>
            {
                { "Id", 8 },
                { "Payload", "{}" }  // Empty JSON object
            }
        };

        // Act
        using var reader = new MySQLReader(new FakeDataReader(data));
        reader.Read();
        var model = reader.ToModel<Telemetria>();

        // Assert
        Assert.Equal(8, model.Id);
        Assert.NotNull(model.Payload);
        Assert.Equal(0, model.Payload.Lat);  // Default values
        Assert.Equal(0, model.Payload.Lng);
    }
}
