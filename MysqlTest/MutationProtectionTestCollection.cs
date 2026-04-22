using Xunit;

namespace MysqlTest;

/// <summary>
/// Agrupa testes que leem ou modificam `MySQL.DefaultOptions.MutationProtection`,
/// forçando execução serial para evitar corrida pelo estado global.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public class MutationProtectionTestCollection
{
    public const string Name = "MutationProtectionTests";
}
