using System;
using Microsoft.Extensions.Logging;

namespace Jovemnf.MySQL;

public sealed class MySQLOptions
{
    public ILogger Logger { get; set; }
    public MySQLMutationProtectionOptions MutationProtection { get; set; } = new();
    public MySQLBulkOptions Bulk { get; set; } = new();
    public Func<string, string> SqlMasker { get; set; } = static sql => sql;
}

public sealed class MySQLMutationProtectionOptions
{
    public bool RequireConfirmationForAllOperations { get; set; }
    public bool RequireLimitForDeleteAllOperations { get; set; }
    public bool RequireLimitForUpdateAllOperations { get; set; }
}

public sealed class MySQLBulkOptions
{
    public int DefaultChunkSize { get; set; } = 500;
}

public sealed class MySQLCommandLogContext
{
    public string Operation { get; init; }
    public string Sql { get; init; }
    public string DebugSql { get; init; }
    public TimeSpan Duration { get; init; }
    public int? RowsAffected { get; init; }
    public bool HasTransaction { get; init; }
    public Exception Exception { get; init; }
}
