using System;

namespace Jovemnf.MySQL;

/// <summary>
/// Configuração do pool de conexões
/// </summary>
public class PoolConfiguration
{
    /// <summary>
    /// Número mínimo de conexões mantidas no pool
    /// </summary>
    public uint MinPoolSize { get; set; } = 2;
    
    /// <summary>
    /// Número máximo de conexões permitidas no pool
    /// </summary>
    public uint MaxPoolSize { get; set; } = 100;
    
    /// <summary>
    /// Tempo máximo de espera para obter uma conexão do pool
    /// </summary>
    public uint ConnectionTimeout { get; set; } = 30;
    
    /// <summary>
    /// Tempo máximo que uma conexão pode ficar inativa antes de ser removida
    /// </summary>
    public uint IdleTimeout { get; set; } = 20;
    
    /// <summary>
    /// Whether connections are reset when being retrieved from the pool.
    /// </summary>
    public bool ConnectionReset { get; set; } = true;
    
    /// <summary>
    /// The maximum lifetime (in seconds) for any connection, or 0 for no lifetime limit.
    /// </summary>
    public uint ConnectionLifeTime { get; set; } = 0;
    
    /// <summary>
    /// CP Keepalive idle time (in seconds), or 0 to use OS defaults.
    /// </summary>
    public uint KeepaliveInterval { get; set; } = 0;
    
}
