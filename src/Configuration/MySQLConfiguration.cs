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
    public string Host { get; set; }

    /// <summary>
    /// Nome do banco de dados.
    /// </summary>
    public string Database { get; set; }

    /// <summary>
    /// Nome de usuário para autenticação.
    /// </summary>
    public string Username { get; set; }

    /// <summary>
    /// Senha para autenticação.
    /// </summary>
    public string Password { get; set; }

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
    public string ConnectionString { get; set; }

    /// <summary>
    /// Timezone padrão da sessão MySQL para esta conexão.
    /// Exemplos: "+00:00", "UTC", "SYSTEM" ou "America/Sao_Paulo".
    /// </summary>
    public string SessionTimeZone { get; set; }
}