namespace Jovemnf.MySQL.Configuration;

/// <summary>
/// Classe de configuração para conexão MySQL.
/// Agrupa todas as propriedades necessárias para estabelecer uma conexão.
/// </summary>
public class MySQLConfiguration
{
    /// <summary>
    /// Host ou endereço IP do servidor MySQL.
    /// </summary>
    public string? Host { get; set; }

    /// <summary>
    /// Nome do banco de dados.
    /// </summary>
    public string? Database { get; set; }

    /// <summary>
    /// Nome de usuário para autenticação.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Senha para autenticação.
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Porta do servidor MySQL. Padrão: 3306.
    /// </summary>
    public uint Port { get; set; } = 3306;

    /// <summary>
    /// Charset a ser usado na conexão. Padrão: utf8.
    /// </summary>
    public string Charset { get; set; } = "utf8";

    /// <summary>
    /// String de conexão completa. Se fornecida, outras propriedades são ignoradas.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Tag (identificador) desta configuração para cenários de sharding ou múltiplas conexões (ex: "Master", 1, "TenantA").
    /// Ao receber ints, enums ou outros objetos, converte para string internamente.
    /// </summary>
    private string? _tag = "Default";
    public object? Tag 
    { 
        get => _tag; 
        init => _tag = value?.ToString(); 
    }

    /// <summary>
    /// Indica se esta é a configuração padrão.
    /// </summary>
    public bool IsDefault { get; set; } = false;

    /// <summary>
    /// Configuração de pool específica para este shard.
    /// Se null, usa as configurações globais (propriedades estáticas da classe MySQL).
    /// </summary>
    public PoolConfiguration? Pool { get; set; }
}
