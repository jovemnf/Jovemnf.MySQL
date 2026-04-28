using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Jovemnf.MySQL.Builder;

namespace Jovemnf.MySQL;

/// <summary>
/// Classe base abstrata para entidades mapeadas para tabelas MySQL.
/// Usa o padrão CRTP (<c>class Veiculo : Entity&lt;Veiculo&gt;</c>) para expor
/// atalhos estáticos no estilo LINQ diretamente no tipo derivado, por exemplo:
/// <code>
/// var builder = Veiculo.Where(v => v.IdVeiculo == 1);
/// var lista   = await Veiculo.Where(v => v.Ativo).ExecuteAsync(connection);
/// </code>
/// </summary>
/// <typeparam name="TSelf">O próprio tipo da entidade (auto-referência CRTP).</typeparam>
public abstract class Entity<TSelf> where TSelf : Entity<TSelf>, new()
{
    /// <summary>
    /// Cria um novo <see cref="SelectQueryBuilder{TSelf}"/> para a entidade,
    /// já associado à tabela resolvida pelo <see cref="DbTableAttribute"/>.
    /// </summary>
    public static SelectQueryBuilder<TSelf> Query() => SelectQueryBuilder.For<TSelf>();

    /// <summary>
    /// Cria um <see cref="SelectQueryBuilder{TSelf}"/> iniciando com um filtro tipado.
    /// Exemplo: <c>Veiculo.Where(v => v.IdVeiculo == 1)</c>.
    /// </summary>
    public static SelectQueryBuilder<TSelf> Where(Expression<Func<TSelf, bool>> predicate)
        => SelectQueryBuilder.For<TSelf>().Where(predicate);

    /// <summary>
    /// Cria um <see cref="SelectQueryBuilder{TSelf}"/> com um filtro simples
    /// (campo/valor/operador) já aplicado.
    /// </summary>
    public static SelectQueryBuilder<TSelf> Where(string field, object value, string op = "=")
    {
        var builder = SelectQueryBuilder.For<TSelf>();
        builder.Where(field, value, op);
        return builder;
    }

    /// <summary>
    /// Cria um <see cref="SelectQueryBuilder{TSelf}"/> selecionando apenas os campos informados.
    /// </summary>
    public static SelectQueryBuilder<TSelf> Select(params string[] fields)
    {
        var builder = SelectQueryBuilder.For<TSelf>();
        builder.Select(fields);
        return builder;
    }

    /// <summary>
    /// Cria um <see cref="SelectQueryBuilder{TSelf}"/> ordenando pelo campo informado.
    /// </summary>
    public static SelectQueryBuilder<TSelf> OrderBy(string field, string direction = "ASC")
    {
        var builder = SelectQueryBuilder.For<TSelf>();
        builder.OrderBy(field, direction);
        return builder;
    }

    /// <summary>
    /// Cria um <see cref="SelectQueryBuilder{TSelf}"/> com um LIMIT aplicado.
    /// </summary>
    public static SelectQueryBuilder<TSelf> Limit(int limit, int offset = 0)
    {
        var builder = SelectQueryBuilder.For<TSelf>();
        builder.Limit(limit, offset);
        return builder;
    }

    /// <summary>
    /// Cria um <see cref="UpdateQueryBuilder{TSelf}"/> para a entidade.
    /// </summary>
    public static UpdateQueryBuilder<TSelf> Update() => UpdateQueryBuilder.For<TSelf>();

    /// <summary>
    /// Cria um <see cref="DeleteQueryBuilder{TSelf}"/> para a entidade.
    /// </summary>
    public static DeleteQueryBuilder<TSelf> Delete() => DeleteQueryBuilder.For<TSelf>();

    /// <summary>
    /// Cria um <see cref="InsertQueryBuilder{TSelf}"/> para a entidade.
    /// </summary>
    public static InsertQueryBuilder<TSelf> Insert() => new InsertQueryBuilder<TSelf>();

    /// <summary>
    /// Cria um <see cref="InsertBatchQueryBuilder{TSelf}"/> para a entidade.
    /// </summary>
    public static InsertBatchQueryBuilder<TSelf> InsertBatch() => InsertBatchQueryBuilder.For<TSelf>();

    // ===== Atalhos de execução =====

    /// <summary>
    /// Retorna todos os registros da tabela mapeada para a entidade.
    /// </summary>
    public static Task<List<TSelf>> AllAsync(MySQL connection)
        => SelectQueryBuilder.For<TSelf>().ExecuteAsync<TSelf>(connection);

    /// <summary>
    /// Retorna todos os registros da tabela usando um <see cref="DatabaseHelper"/>.
    /// </summary>
    public static Task<List<TSelf>> AllAsync(DatabaseHelper helper)
        => helper.ExecuteQueryAsync<TSelf>(SelectQueryBuilder.For<TSelf>());

    /// <summary>
    /// Busca registros que satisfaçam o predicado informado.
    /// </summary>
    public static Task<List<TSelf>> FindAsync(
        Expression<Func<TSelf, bool>> predicate,
        MySQL connection)
        => SelectQueryBuilder.For<TSelf>().Where(predicate).ExecuteAsync<TSelf>(connection);

    /// <summary>
    /// Busca registros que satisfaçam o predicado informado usando um <see cref="DatabaseHelper"/>.
    /// </summary>
    public static Task<List<TSelf>> FindAsync(
        Expression<Func<TSelf, bool>> predicate,
        DatabaseHelper helper)
        => helper.ExecuteQueryAsync<TSelf>(SelectQueryBuilder.For<TSelf>().Where(predicate));

    /// <summary>
    /// Verifica se existe pelo menos um registro que satisfaça o predicado informado.
    /// </summary>
    public static Task<bool> ExistsAsync(
        Expression<Func<TSelf, bool>> predicate,
        MySQL connection)
        => SelectQueryBuilder.For<TSelf>().Where(predicate).ExistsAsync(connection);

    /// <summary>
    /// Verifica se existe pelo menos um registro que satisfaça o predicado informado.
    /// </summary>
    public static Task<bool> ExistsAsync(
        Expression<Func<TSelf, bool>> predicate,
        MySQL connection,
        CancellationToken cancellationToken)
        => SelectQueryBuilder.For<TSelf>().Where(predicate).ExistsAsync(connection, cancellationToken);

    /// <summary>
    /// Conta os registros que satisfaçam o predicado informado.
    /// </summary>
    public static Task<long> CountAsync(
        Expression<Func<TSelf, bool>> predicate,
        MySQL connection)
    {
        ArgumentNullException.ThrowIfNull(connection);
        return connection.ExecuteCountAsync(SelectQueryBuilder.For<TSelf>().Where(predicate));
    }

    /// <summary>
    /// Conta todos os registros da tabela mapeada.
    /// </summary>
    public static Task<long> CountAsync(MySQL connection)
    {
        ArgumentNullException.ThrowIfNull(connection);
        return connection.ExecuteCountAsync(SelectQueryBuilder.For<TSelf>());
    }

    // ===== Streaming (IAsyncEnumerable) =====

    /// <summary>
    /// Itera todas as linhas da tabela mapeada em modo streaming, sem carregar tudo em memória.
    /// </summary>
    /// <remarks>
    /// Cada item é yieldado conforme chega do cursor do MySQL. Use <c>await foreach</c> para
    /// garantir o fechamento correto do reader ao final (ou em caso de <c>break</c>/exceção).
    /// </remarks>
    public static IAsyncEnumerable<TSelf> StreamAllAsync(MySQL connection, CancellationToken cancellationToken = default)
        => SelectQueryBuilder.For<TSelf>().StreamAsync<TSelf>(connection, cancellationToken);

    /// <summary>
    /// Versão que utiliza um <see cref="DatabaseHelper"/> (abre/fecha conexão automaticamente).
    /// </summary>
    public static IAsyncEnumerable<TSelf> StreamAllAsync(DatabaseHelper helper, CancellationToken cancellationToken = default)
        => SelectQueryBuilder.For<TSelf>().StreamAsync<TSelf>(helper, cancellationToken);

    /// <summary>
    /// Itera em modo streaming as linhas que satisfazem o predicado informado.
    /// </summary>
    public static IAsyncEnumerable<TSelf> StreamAsync(
        Expression<Func<TSelf, bool>> predicate,
        MySQL connection,
        CancellationToken cancellationToken = default)
        => SelectQueryBuilder.For<TSelf>().Where(predicate).StreamAsync<TSelf>(connection, cancellationToken);

    /// <summary>
    /// Versão de <see cref="StreamAsync(Expression{Func{TSelf, bool}}, MySQL, CancellationToken)"/>
    /// que utiliza um <see cref="DatabaseHelper"/>.
    /// </summary>
    public static IAsyncEnumerable<TSelf> StreamAsync(
        Expression<Func<TSelf, bool>> predicate,
        DatabaseHelper helper,
        CancellationToken cancellationToken = default)
        => SelectQueryBuilder.For<TSelf>().Where(predicate).StreamAsync<TSelf>(helper, cancellationToken);

    // ===== Chave primária / FindByPK =====

    private static readonly PrimaryKeyMapping[] _primaryKeyMappings = ResolvePrimaryKeys();

    /// <summary>
    /// Retorna os nomes das colunas que compõem a chave primária, já ordenados
    /// conforme o <see cref="DbPrimaryKeyAttribute.Order"/>.
    /// </summary>
    public static IReadOnlyList<string> PrimaryKeyColumns
        => Array.AsReadOnly(_primaryKeyMappings.Select(p => p.ColumnName).ToArray());

    /// <summary>
    /// Indica se a entidade possui chave primária composta (mais de uma coluna).
    /// </summary>
    public static bool HasCompositePrimaryKey => _primaryKeyMappings.Length > 1;

    /// <summary>
    /// Busca um único registro pela chave primária.
    /// Suporta tanto chaves primárias simples (informe um único valor) quanto
    /// compostas (informe os valores na ordem definida por <see cref="DbPrimaryKeyAttribute.Order"/>).
    /// Retorna <c>null</c> (default) quando nenhum registro é encontrado.
    /// </summary>
    /// <param name="connection">Conexão MySQL aberta.</param>
    /// <param name="pkValues">Valor(es) da chave primária, na ordem definida pelos atributos.</param>
    /// <exception cref="InvalidOperationException">A entidade não declara <see cref="DbPrimaryKeyAttribute"/>.</exception>
    /// <exception cref="ArgumentException">Quantidade de valores difere do número de colunas da PK.</exception>
    public static async Task<TSelf?> FindByPkAsync(MySQL connection, params object[] pkValues)
    {
        var builder = BuildFindByPkQuery(pkValues);
        ArgumentNullException.ThrowIfNull(connection);
        var list = await builder.ExecuteAsync<TSelf>(connection).ConfigureAwait(false);
        return list.Count > 0 ? list[0] : default;
    }

    /// <summary>
    /// Versão com <see cref="CancellationToken"/> de <see cref="FindByPkAsync(MySQL, object[])"/>.
    /// </summary>
    public static async Task<TSelf?> FindByPkAsync(MySQL connection, CancellationToken cancellationToken, params object[] pkValues)
    {
        var builder = BuildFindByPkQuery(pkValues);
        ArgumentNullException.ThrowIfNull(connection);
        var list = await connection.ExecuteQueryAsync<TSelf>(builder, cancellationToken).ConfigureAwait(false);
        return list.Count > 0 ? list[0] : default;
    }

    /// <summary>
    /// Versão que utiliza um <see cref="DatabaseHelper"/> (abre/fecha conexão automaticamente).
    /// </summary>
    public static async Task<TSelf?> FindByPkAsync(DatabaseHelper helper, params object[] pkValues)
    {
        var builder = BuildFindByPkQuery(pkValues);
        ArgumentNullException.ThrowIfNull(helper);
        var list = await helper.ExecuteQueryAsync<TSelf>(builder).ConfigureAwait(false);
        return list.Count > 0 ? list[0] : default;
    }

    /// <summary>
    /// Busca um registro por chave primária composta informando explicitamente o nome
    /// (ou propriedade) de cada coluna junto do valor. Útil quando não se quer depender
    /// da ordem declarativa do atributo.
    /// </summary>
    /// <param name="connection">Conexão MySQL aberta.</param>
    /// <param name="pkValues">
    /// Dicionário no formato <c>{ "coluna_ou_propriedade", valor }</c>.
    /// Aceita tanto o nome da coluna do banco quanto o nome da propriedade do modelo.
    /// </param>
    public static async Task<TSelf?> FindByPkAsync(MySQL connection, IReadOnlyDictionary<string, object> pkValues)
    {
        var builder = BuildFindByPkQuery(pkValues);
        ArgumentNullException.ThrowIfNull(connection);
        var list = await builder.ExecuteAsync<TSelf>(connection).ConfigureAwait(false);
        return list.Count > 0 ? list[0] : default;
    }

    /// <summary>
    /// Versão de <see cref="FindByPkAsync(MySQL, IReadOnlyDictionary{string, object})"/>
    /// que utiliza um <see cref="DatabaseHelper"/>.
    /// </summary>
    public static async Task<TSelf?> FindByPkAsync(DatabaseHelper helper, IReadOnlyDictionary<string, object> pkValues)
    {
        var builder = BuildFindByPkQuery(pkValues);
        ArgumentNullException.ThrowIfNull(helper);
        var list = await helper.ExecuteQueryAsync<TSelf>(builder).ConfigureAwait(false);
        return list.Count > 0 ? list[0] : default;
    }

    /// <summary>
    /// Verifica se existe um registro com a chave primária informada.
    /// </summary>
    public static Task<bool> ExistsByPkAsync(MySQL connection, params object[] pkValues)
    {
        var builder = BuildFindByPkQuery(pkValues, applyLimit: false);
        ArgumentNullException.ThrowIfNull(connection);
        return builder.ExistsAsync(connection);
    }

    private static SelectQueryBuilder<TSelf> BuildFindByPkQuery(object[] pkValues, bool applyLimit = true)
    {
        EnsurePrimaryKeyDeclared();

        if (pkValues == null || pkValues.Length != _primaryKeyMappings.Length)
        {
            throw new ArgumentException(
                $"A entidade {typeof(TSelf).Name} possui {_primaryKeyMappings.Length} coluna(s) de chave primária " +
                $"({string.Join(", ", _primaryKeyMappings.Select(p => p.ColumnName))}), " +
                $"mas foram informados {pkValues?.Length ?? 0} valor(es).",
                nameof(pkValues));
        }

        var builder = SelectQueryBuilder.For<TSelf>();
        for (int i = 0; i < _primaryKeyMappings.Length; i++)
        {
            builder.Where(_primaryKeyMappings[i].ColumnName, pkValues[i]);
        }
        if (applyLimit)
            builder.Limit(1);
        return builder;
    }

    private static SelectQueryBuilder<TSelf> BuildFindByPkQuery(IReadOnlyDictionary<string, object> pkValues, bool applyLimit = true)
    {
        EnsurePrimaryKeyDeclared();
        ArgumentNullException.ThrowIfNull(pkValues);

        if (pkValues.Count != _primaryKeyMappings.Length)
        {
            throw new ArgumentException(
                $"A entidade {typeof(TSelf).Name} possui {_primaryKeyMappings.Length} coluna(s) de chave primária, " +
                $"mas foram informados {pkValues.Count} valor(es).",
                nameof(pkValues));
        }

        var builder = SelectQueryBuilder.For<TSelf>();
        foreach (var pk in _primaryKeyMappings)
        {
            if (!TryGetPrimaryKeyValue(pkValues, pk, out var value))
            {
                throw new ArgumentException(
                    $"Valor da coluna de chave primária '{pk.ColumnName}' (propriedade '{pk.PropertyName}') não foi informado.",
                    nameof(pkValues));
            }
            builder.Where(pk.ColumnName, value);
        }
        if (applyLimit)
            builder.Limit(1);
        return builder;
    }

    private static bool TryGetPrimaryKeyValue(IReadOnlyDictionary<string, object> values, PrimaryKeyMapping pk, [MaybeNullWhen(false)] out object value)
    {
        foreach (var pair in values)
        {
            if (string.Equals(pair.Key, pk.ColumnName, StringComparison.OrdinalIgnoreCase)
                || string.Equals(pair.Key, pk.PropertyName, StringComparison.OrdinalIgnoreCase))
            {
                value = pair.Value;
                return true;
            }
        }
        value = null;
        return false;
    }

    private static void EnsurePrimaryKeyDeclared()
    {
        if (_primaryKeyMappings.Length == 0)
        {
            throw new InvalidOperationException(
                $"A entidade {typeof(TSelf).Name} não possui nenhuma propriedade marcada com [DbPrimaryKey]. " +
                "Marque as propriedades que compõem a chave primária para usar FindByPkAsync/ExistsByPkAsync.");
        }
    }

    private static PrimaryKeyMapping[] ResolvePrimaryKeys()
    {
        var list = new List<PrimaryKeyMapping>();
        var properties = typeof(TSelf).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        for (int i = 0; i < properties.Length; i++)
        {
            var property = properties[i];
            var pkAttr = property.GetCustomAttribute<DbPrimaryKeyAttribute>(true);
            if (pkAttr == null) continue;

            var fieldAttr = property.GetCustomAttribute<DbFieldAttribute>(true);
            var columnName = fieldAttr?.Name ?? property.Name.ToSnakeCase();

            list.Add(new PrimaryKeyMapping(property.Name, columnName, pkAttr.Order, i));
        }

        // OrderBy é estável: em empates de Order, mantém a ordem de declaração (DeclarationIndex).
        list.Sort((a, b) =>
        {
            var cmp = a.Order.CompareTo(b.Order);
            return cmp != 0 ? cmp : a.DeclarationIndex.CompareTo(b.DeclarationIndex);
        });

        return list.ToArray();
    }

    private sealed record PrimaryKeyMapping(string PropertyName, string ColumnName, int Order, int DeclarationIndex);
}
