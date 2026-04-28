using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Jovemnf.MySQL.Configuration;

/// <summary>
/// Gerenciador de configurações para cenários de Sharding ou Múltiplas Conexões.
/// Permite armazenar e recuperar diferentes MySQLConfiguration baseados em uma Tag.
/// Thread-safe: usa ConcurrentDictionary e cache atômico para o shard default.
/// </summary>
public class MySQLShardConfiguration
{
    private readonly ConcurrentDictionary<string, MySQLConfiguration> _shards = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Cache do shard default. Invalidado automaticamente ao adicionar/remover shards.
    /// </summary>
    private volatile MySQLConfiguration _cachedDefault;

    /// <summary>
    /// Indica se existem shards configurados.
    /// </summary>
    public bool HasShards => !_shards.IsEmpty;

    /// <summary>
    /// Adiciona uma nova configuração de shard.
    /// </summary>
    /// <param name="config">A configuração do MySQL que contém a Tag preenchida.</param>
    public void AddShard(MySQLConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(config);
        var tagStr = config.Tag?.ToString();
        if (string.IsNullOrWhiteSpace(tagStr)) throw new ArgumentException("A configuração deve possuir uma Tag definida.", nameof(config));

        _shards[tagStr] = config;

        // Invalida o cache do default para recalcular na próxima chamada
        Interlocked.Exchange(ref _cachedDefault, null);
    }

    /// <summary>
    /// Recupera uma configuração baseada na Tag informada.
    /// </summary>
    /// <param name="tag">A tag do shard desejado. Pode ser string, int, etc.</param>
    /// <returns>A configuração correspondente à tag.</returns>
    public MySQLConfiguration GetShard(object tag)
    {
        var tagStr = tag.ToString();
        if (string.IsNullOrWhiteSpace(tagStr)) throw new ArgumentNullException(nameof(tag));

        if (_shards.TryGetValue(tagStr, out var config))
        {
            return config;
        }

        throw new KeyNotFoundException($"Nenhuma configuração encontrada para a tag '{tagStr}'.");
    }

    /// <summary>
    /// Tenta recuperar uma configuração baseada na Tag informada, sem lançar exceção.
    /// </summary>
    /// <param name="tag">A tag do shard desejado.</param>
    /// <param name="config">A configuração encontrada, ou null.</param>
    /// <returns>True se encontrou, false caso contrário.</returns>
    public bool TryGetShard(object tag, out MySQLConfiguration? config)
    {
        config = null;
        var tagStr = tag.ToString();
        return !string.IsNullOrWhiteSpace(tagStr) && _shards.TryGetValue(tagStr, out config);
    }

    /// <summary>
    /// Recupera a configuração marcada como Default, se houver.
    /// Caso nenhuma esteja explícita como IsDefault = true, retorna a primeira ou a que tenha Tag "Default".
    /// O resultado é cacheado e invalidado automaticamente ao adicionar novos shards.
    /// </summary>
    /// <returns>A configuração padrão.</returns>
    public MySQLConfiguration GetDefaultShard()
    {
        var cached = _cachedDefault;
        if (cached != null) return cached;

        var resolved = _shards.Values.FirstOrDefault(cfg => cfg.IsDefault);

        if (resolved == null)
        {
            _shards.TryGetValue("Default", out resolved);
        }

        if (resolved == null)
        {
            // Pega o primeiro disponível
            using var enumerator = _shards.Values.GetEnumerator();
            if (enumerator.MoveNext())
            {
                resolved = enumerator.Current;
            }
        }

        if (resolved == null)
            throw new InvalidOperationException("Nenhum shard foi configurado.");

        Interlocked.CompareExchange(ref _cachedDefault, resolved, null);
        return resolved;
    }

    /// <summary>
    /// Remove um shard pela Tag.
    /// </summary>
    /// <param name="tag">A tag do shard a remover.</param>
    /// <returns>True se foi removido, false se não existia.</returns>
    public bool RemoveShard(object tag)
    {
        var tagStr = tag?.ToString();
        if (string.IsNullOrWhiteSpace(tagStr)) return false;

        var removed = _shards.TryRemove(tagStr, out _);
        if (removed)
        {
            Interlocked.Exchange(ref _cachedDefault, null);
        }

        return removed;
    }

    /// <summary>
    /// Retorna todas as configurações registradas.
    /// </summary>
    public IEnumerable<MySQLConfiguration> GetAllShards() => _shards.Values;
}