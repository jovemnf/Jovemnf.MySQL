using System;

namespace Jovemnf.MySQL;

/// <summary>
/// Marca uma propriedade como parte da chave primária da tabela mapeada.
/// Funciona tanto para chaves primárias simples (uma única coluna) quanto para
/// chaves primárias compostas (múltiplas colunas).
/// </summary>
/// <remarks>
/// Em chaves primárias compostas, utilize a propriedade <see cref="Order"/> para
/// definir a ordem em que os valores devem ser informados em métodos como
/// <c>Entity&lt;TSelf&gt;.FindByPkAsync</c>. Valores menores vêm primeiro.
/// Se duas colunas tiverem o mesmo <see cref="Order"/>, prevalece a ordem de
/// declaração das propriedades no modelo.
/// </remarks>
/// <example>
/// <code>
/// // Chave primária simples
/// [DbPrimaryKey]
/// [DbField("id_veiculo")]
/// public int IdVeiculo { get; set; }
///
/// // Chave primária composta
/// [DbPrimaryKey(Order = 0)]
/// public int IdUsuario { get; set; }
///
/// [DbPrimaryKey(Order = 1)]
/// public int IdPermissao { get; set; }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public sealed class DbPrimaryKeyAttribute : Attribute
{
    /// <summary>
    /// Posição da coluna em chaves primárias compostas. Valores menores vêm primeiro.
    /// Irrelevante quando a chave primária envolve apenas uma coluna. Padrão: <c>0</c>.
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Inicializa o atributo usando <see cref="Order"/> igual a zero.
    /// </summary>
    public DbPrimaryKeyAttribute() : this(0) { }

    /// <summary>
    /// Inicializa o atributo com a ordem informada para uso em chaves primárias compostas.
    /// </summary>
    /// <param name="order">Posição da coluna na chave primária composta.</param>
    public DbPrimaryKeyAttribute(int order) => Order = order;
}
