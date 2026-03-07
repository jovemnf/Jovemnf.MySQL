using System;
using System.Globalization;

namespace Jovemnf.MySQL;

internal static class DateTimeTimeZoneConverter
{
    internal static DateTime Convert(DateTime value, string sourceTimeZone, string targetTimeZone)
    {
        var normalizedSource = MySqlSessionTimeZone.Normalize(sourceTimeZone);
        var normalizedTarget = MySqlSessionTimeZone.Normalize(targetTimeZone);

        if (string.IsNullOrEmpty(normalizedSource) || string.IsNullOrEmpty(normalizedTarget))
        {
            throw new InvalidOperationException(
                "Configure os timezones com UseDateTimeTimeZone(sourceTimeZone, targetTimeZone) antes de usar WhereDateTime/WhereBetweenDateTime.");
        }

        var sourceDateTime = DateTime.SpecifyKind(value, DateTimeKind.Unspecified);
        if (string.Equals(normalizedSource, normalizedTarget, StringComparison.OrdinalIgnoreCase))
        {
            return sourceDateTime;
        }

        var sourceZone = ResolveTimeZone(normalizedSource);
        var targetZone = ResolveTimeZone(normalizedTarget);

        if (sourceZone.IsInvalidTime(sourceDateTime))
        {
            throw new ArgumentException(
                $"O horário '{sourceDateTime:yyyy-MM-dd HH:mm:ss}' é inválido para o timezone '{normalizedSource}'.",
                nameof(value));
        }

        var sourceOffset = sourceZone.GetUtcOffset(sourceDateTime);
        var sourceDateTimeOffset = new DateTimeOffset(sourceDateTime, sourceOffset);
        var converted = TimeZoneInfo.ConvertTime(sourceDateTimeOffset, targetZone);

        return DateTime.SpecifyKind(converted.DateTime, DateTimeKind.Unspecified);
    }

    internal static object ConvertObject(object value, string sourceTimeZone, string targetTimeZone)
    {
        if (value is DateTime dateTime)
        {
            return Convert(dateTime, sourceTimeZone, targetTimeZone);
        }

        if (value is DateTimeOffset dateTimeOffset)
        {
            return Convert(dateTimeOffset.DateTime, sourceTimeZone, targetTimeZone);
        }

        return value;
    }

    private static TimeZoneInfo ResolveTimeZone(string timeZone)
    {
        if (string.Equals(timeZone, "SYSTEM", StringComparison.OrdinalIgnoreCase))
        {
            return TimeZoneInfo.Local;
        }

        if (string.Equals(timeZone, "UTC", StringComparison.OrdinalIgnoreCase))
        {
            return TimeZoneInfo.Utc;
        }

        if (TryParseOffset(timeZone, out var offset))
        {
            var sign = offset >= TimeSpan.Zero ? "+" : "-";
            var id = $"UTC{sign}{offset.Duration():hh\\:mm}";
            return TimeZoneInfo.CreateCustomTimeZone(id, offset, id, id);
        }

        return TimeZoneInfo.FindSystemTimeZoneById(timeZone);
    }

    private static bool TryParseOffset(string value, out TimeSpan offset)
    {
        offset = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = value.Trim();
        if (normalized.Length != 6 || (normalized[0] != '+' && normalized[0] != '-'))
        {
            return false;
        }

        if (!TimeSpan.TryParseExact(
                normalized.Substring(1),
                "hh\\:mm",
                CultureInfo.InvariantCulture,
                out var parsed))
        {
            return false;
        }

        offset = normalized[0] == '-' ? -parsed : parsed;
        return true;
    }
}
