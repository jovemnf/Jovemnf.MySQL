using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace Jovemnf.MySQL;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class DbConverterAttribute(Type converterType) : Attribute
{
    public Type ConverterType { get; } = converterType ?? throw new ArgumentNullException(nameof(converterType));
}

public interface IMySQLValueConverter
{
    object ConvertFromDb(object value, Type targetType);
}

public static class MySQLValueConverterRegistry
{
    private static readonly ConcurrentDictionary<Type, IMySQLValueConverter> Converters = new();

    public static void Register<TTarget>(IMySQLValueConverter converter)
    {
        ArgumentNullException.ThrowIfNull(converter);
        Converters[typeof(TTarget)] = converter;
    }

    public static bool TryConvert(Type targetType, object value, out object convertedValue)
    {
        var resolvedTargetType = Nullable.GetUnderlyingType(targetType) ?? targetType;
        if (Converters.TryGetValue(resolvedTargetType, out var converter))
        {
            convertedValue = converter.ConvertFromDb(value, targetType);
            return true;
        }

        convertedValue = null;
        return false;
    }

    internal static bool TryConvert(MemberInfo member, Type targetType, object value, out object convertedValue)
    {
        var attribute = member?.GetCustomAttribute<DbConverterAttribute>(true);
        if (attribute?.ConverterType == null)
        {
            convertedValue = null;
            return false;
        }

        if (Activator.CreateInstance(attribute.ConverterType) is not IMySQLValueConverter converter)
            throw new InvalidOperationException($"O conversor '{attribute.ConverterType.Name}' deve implementar IMySQLValueConverter.");

        convertedValue = converter.ConvertFromDb(value, targetType);
        return true;
    }

    internal static bool TryConvert(ParameterInfo parameter, Type targetType, object value, out object convertedValue)
    {
        var attribute = parameter?.GetCustomAttribute<DbConverterAttribute>(true);
        if (attribute?.ConverterType == null)
        {
            convertedValue = null;
            return false;
        }

        if (Activator.CreateInstance(attribute.ConverterType) is not IMySQLValueConverter converter)
            throw new InvalidOperationException($"O conversor '{attribute.ConverterType.Name}' deve implementar IMySQLValueConverter.");

        convertedValue = converter.ConvertFromDb(value, targetType);
        return true;
    }
}
