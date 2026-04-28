using Jovemnf.MySQL;
using Jovemnf.MySQL.Configuration;
using Xunit;

namespace MysqlTest;

public class MySQLShardConfigurationTests
{
    #region Helpers

    private static MySQLConfiguration MakeConfig(object tag, bool isDefault = false, string host = "localhost")
    {
        return new MySQLConfiguration
        {
            Tag = tag,
            Host = host,
            Database = $"db_{tag}",
            Username = "user",
            Password = "pwd",
            IsDefault = isDefault
        };
    }

    private static MySQLConfiguration MakeConfigWithConnectionString(object tag, bool isDefault = false)
    {
        return new MySQLConfiguration
        {
            Tag = tag,
            ConnectionString = $"Server=shard-{tag};Database=db_{tag};User ID=user;Password=pwd;",
            IsDefault = isDefault
        };
    }

    #endregion

    #region HasShards

    [Fact]
    public void HasShards_EmptyByDefault()
    {
        var manager = new MySQLShardConfiguration();
        Assert.False(manager.HasShards);
    }

    [Fact]
    public void HasShards_TrueAfterAdd()
    {
        var manager = new MySQLShardConfiguration();
        manager.AddShard(MakeConfig("Master"));
        Assert.True(manager.HasShards);
    }

    [Fact]
    public void HasShards_FalseAfterRemoveAll()
    {
        var manager = new MySQLShardConfiguration();
        manager.AddShard(MakeConfig("Master"));
        manager.RemoveShard("Master");
        Assert.False(manager.HasShards);
    }

    #endregion

    #region AddShard

    [Fact]
    public void AddShard_NullConfig_Throws()
    {
        var manager = new MySQLShardConfiguration();
        Assert.Throws<ArgumentNullException>(() => manager.AddShard(null!));
    }

    [Fact]
    public void AddShard_WithStringTag_Succeeds()
    {
        var manager = new MySQLShardConfiguration();
        var config = MakeConfig("Shard1");
        manager.AddShard(config);

        var result = manager.GetShard("Shard1");
        Assert.Same(config, result);
    }

    [Fact]
    public void AddShard_WithIntTag_Succeeds()
    {
        var manager = new MySQLShardConfiguration();
        var config = MakeConfig(42);
        manager.AddShard(config);

        var result = manager.GetShard(42);
        Assert.Same(config, result);
    }

    [Fact]
    public void AddShard_WithEnumTag_Succeeds()
    {
        var manager = new MySQLShardConfiguration();
        var config = MakeConfig(ShardType.ReadOnly);
        manager.AddShard(config);

        var result = manager.GetShard(ShardType.ReadOnly);
        Assert.Same(config, result);
    }

    [Fact]
    public void AddShard_OverwritesExistingTag()
    {
        var manager = new MySQLShardConfiguration();
        var first = MakeConfig("Master", host: "host-a");
        var second = MakeConfig("Master", host: "host-b");

        manager.AddShard(first);
        manager.AddShard(second);

        var result = manager.GetShard("Master");
        Assert.Same(second, result);
        Assert.Equal("host-b", result.Host);
    }

    [Fact]
    public void AddShard_InvalidatesDefaultCache()
    {
        var manager = new MySQLShardConfiguration();
        var first = MakeConfig("A", isDefault: true);
        manager.AddShard(first);

        // Popula o cache
        var cached = manager.GetDefaultShard();
        Assert.Same(first, cached);

        // Adiciona um novo default – cache deve ser invalidado
        var second = MakeConfig("B", isDefault: true);
        manager.AddShard(second);

        var newDefault = manager.GetDefaultShard();
        // O novo default deve ser um dos que tem IsDefault = true
        Assert.True(newDefault.IsDefault);
    }

    #endregion

    #region GetShard

    [Fact]
    public void GetShard_CaseInsensitive()
    {
        var manager = new MySQLShardConfiguration();
        var config = MakeConfig("Master");
        manager.AddShard(config);

        Assert.Same(config, manager.GetShard("master"));
        Assert.Same(config, manager.GetShard("MASTER"));
        Assert.Same(config, manager.GetShard("Master"));
    }

    [Fact]
    public void GetShard_TagNotFound_ThrowsKeyNotFoundException()
    {
        var manager = new MySQLShardConfiguration();
        manager.AddShard(MakeConfig("Master"));

        var ex = Assert.Throws<KeyNotFoundException>(() => manager.GetShard("Slave"));
        Assert.Contains("Slave", ex.Message);
    }

    [Fact]
    public void GetShard_IntTag_MatchesToString()
    {
        var manager = new MySQLShardConfiguration();
        var config = MakeConfig(10);
        manager.AddShard(config);

        // Buscar com int
        Assert.Same(config, manager.GetShard(10));
        // Buscar com string equivalente
        Assert.Same(config, manager.GetShard("10"));
    }

    #endregion

    #region TryGetShard

    [Fact]
    public void TryGetShard_ExistingTag_ReturnsTrue()
    {
        var manager = new MySQLShardConfiguration();
        var config = MakeConfig("Shard1");
        manager.AddShard(config);

        var found = manager.TryGetShard("Shard1", out var result);

        Assert.True(found);
        Assert.Same(config, result);
    }

    [Fact]
    public void TryGetShard_MissingTag_ReturnsFalse()
    {
        var manager = new MySQLShardConfiguration();
        manager.AddShard(MakeConfig("Shard1"));

        var found = manager.TryGetShard("Inexistente", out var result);

        Assert.False(found);
        Assert.Null(result);
    }

    [Fact]
    public void TryGetShard_IntTag_Works()
    {
        var manager = new MySQLShardConfiguration();
        manager.AddShard(MakeConfig(5));

        Assert.True(manager.TryGetShard(5, out var result));
        Assert.NotNull(result);
        Assert.Equal("5", result!.Tag?.ToString());
    }

    #endregion

    #region GetDefaultShard

    [Fact]
    public void GetDefaultShard_NoShards_Throws()
    {
        var manager = new MySQLShardConfiguration();
        Assert.Throws<InvalidOperationException>(() => manager.GetDefaultShard());
    }

    [Fact]
    public void GetDefaultShard_ReturnsIsDefaultTrue()
    {
        var manager = new MySQLShardConfiguration();
        var nonDefault = MakeConfig("A");
        var defaultCfg = MakeConfig("B", isDefault: true);

        manager.AddShard(nonDefault);
        manager.AddShard(defaultCfg);

        var result = manager.GetDefaultShard();
        Assert.Same(defaultCfg, result);
    }

    [Fact]
    public void GetDefaultShard_FallsBackToTagDefault()
    {
        var manager = new MySQLShardConfiguration();
        var config = MakeConfig("Default");
        var other = MakeConfig("Other");

        manager.AddShard(other);
        manager.AddShard(config);

        var result = manager.GetDefaultShard();
        Assert.Same(config, result);
    }

    [Fact]
    public void GetDefaultShard_FallsBackToFirstWhenNoDefaultTag()
    {
        var manager = new MySQLShardConfiguration();
        var first = MakeConfig("Alpha");

        manager.AddShard(first);

        var result = manager.GetDefaultShard();
        // Deve retornar algum shard, já que não há nenhum com IsDefault ou Tag "Default"
        Assert.NotNull(result);
    }

    [Fact]
    public void GetDefaultShard_CachesResult()
    {
        var manager = new MySQLShardConfiguration();
        var config = MakeConfig("Master", isDefault: true);
        manager.AddShard(config);

        var first = manager.GetDefaultShard();
        var second = manager.GetDefaultShard();

        // Mesma referência, cache funcionando
        Assert.Same(first, second);
    }

    [Fact]
    public void GetDefaultShard_IsDefault_HasPriorityOverTagDefault()
    {
        var manager = new MySQLShardConfiguration();
        var tagDefault = MakeConfig("Default");
        var explicitDefault = MakeConfig("Relatorios", isDefault: true);

        manager.AddShard(tagDefault);
        manager.AddShard(explicitDefault);

        var result = manager.GetDefaultShard();
        Assert.Same(explicitDefault, result);
    }

    #endregion

    #region RemoveShard

    [Fact]
    public void RemoveShard_ExistingTag_ReturnsTrue()
    {
        var manager = new MySQLShardConfiguration();
        manager.AddShard(MakeConfig("Master"));

        Assert.True(manager.RemoveShard("Master"));
        Assert.False(manager.HasShards);
    }

    [Fact]
    public void RemoveShard_MissingTag_ReturnsFalse()
    {
        var manager = new MySQLShardConfiguration();
        Assert.False(manager.RemoveShard("NaoExiste"));
    }

    [Fact]
    public void RemoveShard_IntTag_Works()
    {
        var manager = new MySQLShardConfiguration();
        manager.AddShard(MakeConfig(99));

        Assert.True(manager.RemoveShard(99));
        Assert.False(manager.TryGetShard(99, out _));
    }

    [Fact]
    public void RemoveShard_InvalidatesDefaultCache()
    {
        var manager = new MySQLShardConfiguration();
        var defaultCfg = MakeConfig("Master", isDefault: true);
        var backup = MakeConfig("Backup");

        manager.AddShard(defaultCfg);
        manager.AddShard(backup);

        // Popula cache
        var cached = manager.GetDefaultShard();
        Assert.Same(defaultCfg, cached);

        // Remove o default
        manager.RemoveShard("Master");

        // Agora deve retornar o backup
        var newDefault = manager.GetDefaultShard();
        Assert.Same(backup, newDefault);
    }

    #endregion

    #region GetAllShards

    [Fact]
    public void GetAllShards_Empty_ReturnsEmpty()
    {
        var manager = new MySQLShardConfiguration();
        Assert.Empty(manager.GetAllShards());
    }

    [Fact]
    public void GetAllShards_ReturnsAllAdded()
    {
        var manager = new MySQLShardConfiguration();
        manager.AddShard(MakeConfig("A"));
        manager.AddShard(MakeConfig("B"));
        manager.AddShard(MakeConfig("C"));

        var all = manager.GetAllShards().ToList();
        Assert.Equal(3, all.Count);
    }

    #endregion

    #region MySQLConfiguration.Tag

    [Fact]
    public void Tag_DefaultValue_IsDefault()
    {
        var config = new MySQLConfiguration();
        Assert.Equal("Default", config.Tag?.ToString());
    }

    [Fact]
    public void Tag_AcceptsString()
    {
        var config = new MySQLConfiguration { Tag = "Master" };
        Assert.Equal("Master", config.Tag?.ToString());
    }

    [Fact]
    public void Tag_AcceptsInt()
    {
        var config = new MySQLConfiguration { Tag = 42 };
        Assert.Equal("42", config.Tag?.ToString());
    }

    [Fact]
    public void Tag_AcceptsEnum()
    {
        var config = new MySQLConfiguration { Tag = ShardType.ReadOnly };
        Assert.Equal("ReadOnly", config.Tag?.ToString());
    }

    #endregion

    #region MySQL.FromShard (integração com construtores)

    [Fact]
    public void FromShard_WithConnectionString_CreatesInstance()
    {
        // Usa uma instância local para não interferir com GlobalShards
        var manager = new MySQLShardConfiguration();
        manager.AddShard(MakeConfigWithConnectionString("Test1", isDefault: true));

        var mysql = new MySQL(manager, "Test1");
        Assert.NotNull(mysql);
    }

    [Fact]
    public void FromShard_DefaultTag_CreatesInstance()
    {
        var manager = new MySQLShardConfiguration();
        manager.AddShard(MakeConfigWithConnectionString("MyDefault", isDefault: true));

        // tag = null deve usar o default
        var mysql = new MySQL(manager);
        Assert.NotNull(mysql);
    }

    [Fact]
    public void Constructor_WithShardConfig_IntTag()
    {
        var manager = new MySQLShardConfiguration();
        manager.AddShard(MakeConfigWithConnectionString(10));

        var mysql = new MySQL(manager, 10);
        Assert.NotNull(mysql);
    }

    [Fact]
    public void Constructor_WithShardConfig_EnumTag()
    {
        var manager = new MySQLShardConfiguration();
        manager.AddShard(MakeConfigWithConnectionString(ShardType.Primary));

        var mysql = new MySQL(manager, ShardType.Primary);
        Assert.NotNull(mysql);
    }

    [Fact]
    public void Constructor_WithShardConfig_InvalidTag_Throws()
    {
        var manager = new MySQLShardConfiguration();
        manager.AddShard(MakeConfigWithConnectionString("Master"));

        Assert.Throws<KeyNotFoundException>(() => new MySQL(manager, "Inexistente"));
    }

    #endregion

    #region Thread-safety (básico)

    [Fact]
    public async Task ConcurrentAddAndGet_DoesNotThrow()
    {
        var manager = new MySQLShardConfiguration();
        var tasks = new List<Task>();

        for (int i = 0; i < 100; i++)
        {
            int captured = i;
            tasks.Add(Task.Run(() =>
            {
                manager.AddShard(MakeConfig(captured));
            }));
        }

        await Task.WhenAll(tasks);

        Assert.Equal(100, manager.GetAllShards().Count());

        // Leitura concorrente
        var readTasks = new List<Task>();
        for (int i = 0; i < 100; i++)
        {
            int captured = i;
            readTasks.Add(Task.Run(() =>
            {
                var found = manager.TryGetShard(captured, out var cfg);
                Assert.True(found);
                Assert.NotNull(cfg);
            }));
        }

        await Task.WhenAll(readTasks);
    }

    [Fact]
    public async Task ConcurrentGetDefaultShard_ReturnsSameReference()
    {
        var manager = new MySQLShardConfiguration();
        manager.AddShard(MakeConfig("Default", isDefault: true));

        var results = new MySQLConfiguration[50];
        var tasks = new List<Task>();

        for (int i = 0; i < 50; i++)
        {
            int captured = i;
            tasks.Add(Task.Run(() =>
            {
                results[captured] = manager.GetDefaultShard();
            }));
        }

        await Task.WhenAll(tasks);

        // Todas devem ser a mesma referência (cache)
        var first = results[0];
        Assert.All(results, r => Assert.Same(first, r));
    }

    #endregion

    /// <summary>
    /// Enum de exemplo para testes com Tags dinâmicas.
    /// </summary>
    private enum ShardType
    {
        Primary,
        ReadOnly,
        Analytics
    }
}
