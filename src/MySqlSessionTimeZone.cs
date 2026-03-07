using MySqlConnector;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Jovemnf.MySQL;

internal static class MySqlSessionTimeZone
{
    private static readonly Regex AllowedTimeZonePattern = new(
        "^(SYSTEM|[A-Za-z0-9_+:/-]+)$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    internal static string Normalize(string sessionTimeZone)
    {
        if (string.IsNullOrWhiteSpace(sessionTimeZone))
        {
            return string.Empty;
        }

        var normalized = sessionTimeZone.Trim();
        if (!AllowedTimeZonePattern.IsMatch(normalized))
        {
            throw new ArgumentException(
                "SessionTimeZone aceita valores como '+00:00', 'UTC', 'SYSTEM' ou 'America/Sao_Paulo'.",
                nameof(sessionTimeZone));
        }

        return normalized;
    }

    internal static string BuildSetTimeZoneCommandText(string sessionTimeZone)
    {
        var normalized = Normalize(sessionTimeZone);
        return string.IsNullOrEmpty(normalized)
            ? string.Empty
            : $"SET time_zone = '{normalized}'";
    }

    internal static void Apply(MySqlConnection connection, string sessionTimeZone)
    {
        ArgumentNullException.ThrowIfNull(connection);

        var commandText = BuildSetTimeZoneCommandText(sessionTimeZone);
        if (string.IsNullOrEmpty(commandText))
        {
            return;
        }

        using var command = new MySqlCommand(commandText, connection);
        command.ExecuteNonQuery();
    }

    internal static async Task ApplyAsync(MySqlConnection connection, string sessionTimeZone)
    {
        ArgumentNullException.ThrowIfNull(connection);

        var commandText = BuildSetTimeZoneCommandText(sessionTimeZone);
        if (string.IsNullOrEmpty(commandText))
        {
            return;
        }

        using var command = new MySqlCommand(commandText, connection);
        await command.ExecuteNonQueryAsync();
    }
}
