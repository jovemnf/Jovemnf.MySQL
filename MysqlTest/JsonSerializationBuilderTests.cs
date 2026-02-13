using System;
using System.Collections.Generic;
using System.Text.Json;
using Jovemnf.MySQL.Builder;
using Xunit;

namespace MysqlTest;

// Test models for JSON serialization
public class PayloadForSerialization
{
    public double Lat { get; set; }
    public double Lng { get; set; }
}

public class AddressForSerialization
{
    public string Street { get; set; }
    public string City { get; set; }
    public string ZipCode { get; set; }
}

public class NestedObjectForSerialization
{
    public int Id { get; set; }
    public string Name { get; set; }
    public AddressForSerialization Address { get; set; }
}

public class JsonSerializationBuilderTests
{
    [Fact]
    public void TestInsertBuilder_ValueAsJson_SimpleObject()
    {
        // Arrange
        var payload = new PayloadForSerialization
        {
            Lat = -23.551,
            Lng = -46.633
        };

        var builder = new InsertQueryBuilder()
            .Table("telemetria_eventos")
            .Value("id", 1)
            .ValueAsJson("payload", payload);

        // Act
        var (sql, command) = builder.Build();

        // Assert
        Assert.Contains("INSERT INTO", sql);
        Assert.Contains("`telemetria_eventos`", sql);
        Assert.Contains("`id`", sql);
        Assert.Contains("`payload`", sql);
        
        // Verify JSON was serialized
        var payloadParam = command.Parameters["@p1"];
        Assert.NotNull(payloadParam);
        
        var jsonString = payloadParam.Value.ToString();
        Assert.Contains("\"Lat\"", jsonString);
        Assert.Contains("-23.551", jsonString);
        Assert.Contains("\"Lng\"", jsonString);
        Assert.Contains("-46.633", jsonString);
    }

    [Fact]
    public void TestInsertBuilder_ValueAsJson_NullValue()
    {
        // Arrange
        var builder = new InsertQueryBuilder()
            .Table("telemetria_eventos")
            .Value("id", 1)
            .ValueAsJson("payload", null);

        // Act
        var (sql, command) = builder.Build();

        // Assert
        var payloadParam = command.Parameters["@p1"];
        Assert.NotNull(payloadParam);
        Assert.Equal(DBNull.Value, payloadParam.Value);
    }

    [Fact]
    public void TestInsertBuilder_ValueAsJson_WithCustomOptions()
    {
        // Arrange
        var payload = new PayloadForSerialization
        {
            Lat = -23.551,
            Lng = -46.633
        };

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        var builder = new InsertQueryBuilder()
            .Table("telemetria_eventos")
            .ValueAsJson("payload", payload, options);

        // Act
        var (sql, command) = builder.Build();

        // Assert
        var payloadParam = command.Parameters["@p0"];
        Assert.NotNull(payloadParam);
        
        var jsonString = payloadParam.Value.ToString();
        // With camelCase policy, properties should be lowercase
        Assert.Contains("\"lat\"", jsonString);
        Assert.Contains("\"lng\"", jsonString);
    }

    [Fact]
    public void TestUpdateBuilder_SetAsJson_SimpleObject()
    {
        // Arrange
        var payload = new PayloadForSerialization
        {
            Lat = -22.123,
            Lng = -45.456
        };

        var builder = new UpdateQueryBuilder()
            .Table("telemetria_eventos")
            .SetAsJson("payload", payload)
            .Where("id", 1);

        // Act
        var (sql, command) = builder.Build();

        // Assert
        Assert.Contains("UPDATE", sql);
        Assert.Contains("`telemetria_eventos`", sql);
        Assert.Contains("SET `payload` = @p0", sql);
        Assert.Contains("WHERE", sql);
        
        // Verify JSON was serialized
        var payloadParam = command.Parameters["@p0"];
        Assert.NotNull(payloadParam);
        
        var jsonString = payloadParam.Value.ToString();
        Assert.Contains("\"Lat\"", jsonString);
        Assert.Contains("-22.123", jsonString);
        Assert.Contains("\"Lng\"", jsonString);
        Assert.Contains("-45.456", jsonString);
    }

    [Fact]
    public void TestUpdateBuilder_SetAsJson_ComplexObject()
    {
        // Arrange
        var nested = new NestedObjectForSerialization
        {
            Id = 10,
            Name = "Test User",
            Address = new AddressForSerialization
            {
                Street = "123 Main St",
                City = "São Paulo",
                ZipCode = "01234-567"
            }
        };

        var builder = new UpdateQueryBuilder()
            .Table("users")
            .SetAsJson("data", nested)
            .Where("id", 10);

        // Act
        var (sql, command) = builder.Build();

        // Assert
        var dataParam = command.Parameters["@p0"];
        Assert.NotNull(dataParam);
        
        var jsonString = dataParam.Value.ToString();
        Assert.Contains("\"Id\"", jsonString);
        Assert.Contains("\"Name\"", jsonString);
        Assert.Contains("\"Address\"", jsonString);
        Assert.Contains("\"Street\"", jsonString);
        Assert.Contains("\"City\"", jsonString);
        Assert.Contains("\"ZipCode\"", jsonString);
        Assert.Contains("01234-567", jsonString);
        // Verify it's valid JSON by deserializing (handles Unicode automatically)
        var deserialized = JsonSerializer.Deserialize<NestedObjectForSerialization>(jsonString);
        Assert.Equal(10, deserialized.Id);
        Assert.Equal("Test User", deserialized.Name);
        Assert.Equal("São Paulo", deserialized.Address.City);
    }

    [Fact]
    public void TestUpdateBuilder_SetAsJson_NullValue()
    {
        // Arrange
        var builder = new UpdateQueryBuilder()
            .Table("telemetria_eventos")
            .SetAsJson("payload", null)
            .Where("id", 1);

        // Act
        var (sql, command) = builder.Build();

        // Assert
        var payloadParam = command.Parameters["@p0"];
        Assert.NotNull(payloadParam);
        Assert.Equal(DBNull.Value, payloadParam.Value);
    }

    [Fact]
    public void TestInsertBuilder_MixedJsonAndNormalValues()
    {
        // Arrange
        var payload = new PayloadForSerialization
        {
            Lat = -23.551,
            Lng = -46.633
        };

        var builder = new InsertQueryBuilder()
            .Table("telemetria_eventos")
            .Value("id", 1)
            .Value("name", "Test Event")
            .ValueAsJson("payload", payload)
            .Value("created_at", DateTime.Now);

        // Act
        var (sql, command) = builder.Build();

        // Assert
        Assert.Contains("INSERT INTO", sql);
        Assert.Contains("`id`", sql);
        Assert.Contains("`name`", sql);
        Assert.Contains("`payload`", sql);
        Assert.Contains("`created_at`", sql);
        
        // Verify we have 4 parameters
        Assert.Equal(4, command.Parameters.Count);
        
        // Verify payload is JSON
        var payloadParam = command.Parameters["@p2"];
        var jsonString = payloadParam.Value.ToString();
        Assert.Contains("\"Lat\"", jsonString);
    }

    [Fact]
    public void TestUpdateBuilder_MixedJsonAndNormalValues()
    {
        // Arrange
        var payload = new PayloadForSerialization
        {
            Lat = -22.0,
            Lng = -45.0
        };

        var builder = new UpdateQueryBuilder()
            .Table("telemetria_eventos")
            .Set("name", "Updated Event")
            .SetAsJson("payload", payload)
            .Set("updated_at", DateTime.Now)
            .Where("id", 1);

        // Act
        var (sql, command) = builder.Build();

        // Assert
        Assert.Contains("UPDATE", sql);
        Assert.Contains("`name` = @p0", sql);
        Assert.Contains("`payload` = @p1", sql);
        Assert.Contains("`updated_at` = @p2", sql);
        
        // Verify payload is JSON
        var payloadParam = command.Parameters["@p1"];
        var jsonString = payloadParam.Value.ToString();
        Assert.Contains("\"Lat\"", jsonString);
        Assert.Contains("-22", jsonString);
    }

    [Fact]
    public void TestInsertBuilder_ValueAsJson_Array()
    {
        // Arrange
        var items = new List<PayloadForSerialization>
        {
            new PayloadForSerialization { Lat = -23.5, Lng = -46.6 },
            new PayloadForSerialization { Lat = -22.9, Lng = -43.2 }
        };

        var builder = new InsertQueryBuilder()
            .Table("locations")
            .ValueAsJson("coordinates", items);

        // Act
        var (sql, command) = builder.Build();

        // Assert
        var coordinatesParam = command.Parameters["@p0"];
        Assert.NotNull(coordinatesParam);
        
        var jsonString = coordinatesParam.Value.ToString();
        Assert.StartsWith("[", jsonString);
        Assert.EndsWith("]", jsonString);
        Assert.Contains("\"Lat\"", jsonString);
        Assert.Contains("-23.5", jsonString);
        Assert.Contains("-22.9", jsonString);
    }
}
