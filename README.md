# Jovemnf.MySQL

Pacote .NET Core de alto desempenho para interação simplificada com bancos de dados MySQL.

## Sumário

- [Instalação](#instalacao)
- [Configuração de conexão](#configuracao)
- [Builders fluentes](#builders-fluentes)
- [Mapeamento e ORM](#mapeamento-e-orm)
- [Operações avançadas](#operacoes-avancadas)
- [DatabaseHelper](#databasehelper)
- [Suporte espacial](#suporte-espacial)
- [Testes](#testes)
- [Troubleshooting](#troubleshooting)
- [Segurança](#seguranca)
- [Licença](#licenca)

## ✨ Características

- **Gerenciamento de Conexão:** Fácil configuração via `MySQLConfiguration`.
- **Query Builders Fluentes:** APIs intuitivas para `SELECT`, `INSERT`, `UPDATE` e `DELETE`.
- **Tipagem Forte:** Enum `QueryOperator` para evitar strings mágicas em operadores SQL.
- **Segurança Nativa:** Proteção robusta contra SQL Injection (parametrização, escape de identificadores e proteção contra mass operations).
- **Mapeamento Automático (ORM):** Conversão automática de resultados para classes C# (POCOs).
- **ExecuteAsync&lt;T&gt;:** Retorno tipado nos builders (Select/Insert/Update/Delete) com resultado já mapeado para o modelo.
- **Suporte a Transações:** Execução atômica de múltiplas operações.
- **Async/Await:** Suporte completo para operações assíncronas.
- **Mapeamento Avançado:** Atributos `DbTable` e `DbField` para controle total sobre nomes de tabelas e colunas, com suporte a `snake_case`.
- **Observabilidade e Cancelamento:** Overloads com `CancellationToken`, logging via `ILogger` e mascaramento de SQL com `MySQLOptions`.
- **Bulk Operations:** `BulkInsertAsync`, `BulkUpsertAsync` e chunking configurável para grandes volumes.
- **Paginação Estruturada:** `PageRequest` e `PagedResult<T>` com metadados de navegação.
- **Tipos Espaciais:** Suporte a `Point`, `Polygon` e helpers geométricos úteis em cenários de rastreamento.

<a id="instalacao"></a>
## 🚀 Instalação

Adicione o pacote ao seu projeto:

```bash
dotnet add package Jovemnf.MySQL
```

## 📖 Como Usar

<a id="configuracao"></a>
### Configuração de Conexão

Você pode configurar a conexão usando o objeto `MySQLConfiguration`:

```csharp
using Jovemnf.MySQL;
using Jovemnf.MySQL.Configuration;

var config = new MySQLConfiguration
{
    Host = "localhost",
    Database = "meu_banco",
    Username = "usuario",
    Password = "senha",
    Port = 3306,
    Charset = "utf8"
};

// Opcional: Inicializar configuração global
MySQL.Init(config);
```

Também é possível configurar o pool globalmente:

```csharp
MySQL.Init(config, new PoolConfiguration
{
    MaxPoolSize = 200,
    MinPoolSize = 10,
    ConnectionTimeout = 15,
    IdleTimeout = 180,
    ConnectionLifeTime = 0,
    ConnectionReset = false,
    KeepaliveInterval = 30
});
```

Se preferir, você também pode instanciar a conexão com connection string diretamente:

```csharp
var mysql = new MySQL("Server=localhost;Database=monitoramento;User ID=usuario;Password=senha;");
await mysql.OpenAsync();
```

<a id="builders-fluentes"></a>
## Builders Fluentes

### Consultas Padrão (Leitura)

```csharp
using (var mysql = new MySQL(config))
{
    await mysql.OpenAsync();
    mysql.OpenCommand("SELECT * FROM usuarios WHERE ativo = @ativo");
    mysql.SetParameter("@ativo", true);
    
    var reader = await mysql.ExecuteQueryAsync();
    while (reader.Read())
    {
        Console.WriteLine(reader.GetString("nome"));
    }
}
```

### Fluent Update Query Builder (Recomendado)

O `UpdateQueryBuilder` permite construir queries de atualização complexas de forma legível e segura.

```csharp
using Jovemnf.MySQL;
using Jovemnf.MySQL.Builder;

var builder = new UpdateQueryBuilder()
    .Table("usuarios")
    .Set("status", "ativo")
    .Set("ultimo_login", DateTime.Now)
    .Where("id", 123);

// Opção 1: Executando via instância do MySQL (Fácil integração)
using (var mysql = new MySQL(config))
{
    await mysql.OpenAsync();
    int rows = await mysql.ExecuteUpdateAsync(builder);
}

// Opção 2: Executando via DatabaseHelper (Gerencia conexão automaticamente)
var helper = new DatabaseHelper(connectionString);
int rows = await helper.ExecuteUpdateAsync(builder);
```

#### Operadores Suportados
O builder suporta diversos operadores: `WhereIn`, `WhereNotIn`, `WhereNull`, `WhereNotNull`, `WhereBetween`, `WhereLike`, e `OrWhere`.

### Fluent Insert Query Builder

O `InsertQueryBuilder` oferece uma forma limpa e segura de inserir dados.

```csharp
var builder = new InsertQueryBuilder()
    .Table("usuarios")
    .Value("nome", "Maria Silva")
    .Value("email", "maria@exemplo.com")
    .Value("ativo", true);

using (var mysql = new MySQL(config))
{
    await mysql.OpenAsync();
    long newId = await mysql.ExecuteInsertAsync(builder);
}
```

#### Upsert em Insert Unitário

Quando você precisa fazer insert com atualização em caso de chave duplicada, o `InsertQueryBuilder` também suporta `ON DUPLICATE KEY UPDATE`:

```csharp
var builder = new InsertQueryBuilder()
    .Table("veiculos")
    .Value("tracker_id", 10)
    .Value("placa", "ABC1234")
    .Value("status", "online")
    .OnDuplicateKeyUpdate("placa", "status");
```

Ou, se preferir atualizar todos os campos exceto alguns:

```csharp
var builder = new InsertQueryBuilder()
    .Table("veiculos")
    .Value("tracker_id", 10)
    .Value("placa", "ABC1234")
    .Value("status", "online")
    .OnDuplicateKeyUpdateAllExcept("tracker_id");
```

### Fluent Insert Batch Query Builder

O `InsertBatchQueryBuilder` permite inserir múltiplas linhas em uma única operação e também suportar `upsert` com `ON DUPLICATE KEY UPDATE`.

```csharp
using Jovemnf.MySQL;
using Jovemnf.MySQL.Builder;

var builder = new InsertBatchQueryBuilder()
    .Table("posicoes")
    .Row(new Dictionary<string, object>
    {
        ["tracker_id"] = 1,
        ["status"] = "online",
        ["velocidade"] = 80
    })
    .Row(new Dictionary<string, object>
    {
        ["tracker_id"] = 2,
        ["status"] = "offline",
        ["velocidade"] = 0
    })
    .OnDuplicateKeyUpdate("status", "velocidade");

using (var mysql = new MySQL(config))
{
    await mysql.OpenAsync();
    int affectedRows = await mysql.ExecuteInsertBatchAsync(builder);
}
```

#### Upsert Atualizando Todos os Campos, Exceto Alguns

Quando quiser atualizar todos os campos do batch em caso de chave duplicada, exceto alguns campos específicos:

```csharp
var builder = new InsertBatchQueryBuilder()
    .Table("posicoes")
    .Row(new Dictionary<string, object>
    {
        ["tracker_id"] = 1,
        ["status"] = "online",
        ["ultima_posicao"] = DateTime.Now
    })
    .OnDuplicateKeyUpdateAllExcept("tracker_id");
```

#### Builder Genérico para Batch

Você também pode usar a versão genérica para mapear automaticamente tabela e colunas a partir de uma classe:

```csharp
[DbTable("veiculos")]
public class Veiculo
{
    [DbField("tracker_id")]
    public int Id { get; set; }

    [DbField("placa")]
    public string Placa { get; set; }

    [DbField("status")]
    public string Status { get; set; }
}

var builder = InsertBatchQueryBuilder.For<Veiculo>()
    .RowsFrom(new[]
    {
        new Veiculo { Id = 1, Placa = "ABC1234", Status = "ativo" },
        new Veiculo { Id = 2, Placa = "DEF5678", Status = "inativo" }
    })
    .OnDuplicateKeyUpdate("Status");

var helper = new DatabaseHelper(connectionString);
int affectedRows = await helper.ExecuteInsertBatchAsync(builder);
```

#### Criando Rows a Partir de um Array ou Lista com Função

Se você já tiver um array ou `List<T>` e quiser transformar cada item em uma linha do batch sem fazer o `foreach` manualmente, use o overload `Rows(items, map)`:

```csharp
var itens = new[]
{
    new { TrackerId = 1, Status = "online", Velocidade = 80 },
    new { TrackerId = 2, Status = "offline", Velocidade = 0 }
};

var builder = new InsertBatchQueryBuilder()
    .Table("posicoes")
    .Rows(itens, item => new Dictionary<string, object>
    {
        ["tracker_id"] = item.TrackerId,
        ["status"] = item.Status,
        ["velocidade"] = item.Velocidade
    })
    .OnDuplicateKeyUpdate("status", "velocidade");
```

#### Regras do Batch

- Todas as linhas do batch devem possuir o mesmo conjunto de colunas.
- O retorno de `ExecuteInsertBatchAsync(...)` é a quantidade de linhas afetadas.
- Diferente do `InsertQueryBuilder`, o batch não usa `LAST_INSERT_ID()` nem `ExecuteAsync<T>()`, pois isso não é confiável para múltiplas linhas.
- Os mesmos mapeamentos de `DbTable`, `DbField`, `snake_case`, JSON e tipos geométricos continuam disponíveis no fluxo batch.

#### Bulk insert e bulk upsert com chunking

Para processar grandes volumes, a classe `MySQL` agora expõe helpers de bulk com chunking automático:

```csharp
using var mysql = new MySQL(config);
await mysql.OpenAsync();

int inserted = await mysql.BulkInsertAsync(veiculos, chunkSize: 500);
int upserted = await mysql.BulkUpsertAsync(veiculos, new[] { "Status", "Placa" }, chunkSize: 500);
int upsertedAllExcept = await mysql.BulkUpsertAllExceptAsync(veiculos, new[] { "Id" }, chunkSize: 500);
```

Se quiser definir o chunk padrão global ou por instância:

```csharp
var options = new MySQLOptions
{
    Bulk = new MySQLBulkOptions
    {
        DefaultChunkSize = 1000
    }
};

using var mysql = new MySQL(config, options);
await mysql.OpenAsync();
await mysql.BulkInsertAsync(veiculos); // usa DefaultChunkSize = 1000
```

### Fluent Select Query Builder

O `SelectQueryBuilder` permite criar consultas de seleção complexas com joins, filtros e ordenação.

```csharp
var builder = new SelectQueryBuilder()
    .Select("u.nome", "c.nome as categoria")
    .From("usuarios u")
    .Join("categorias c", "u.categoria_id", "=", "c.id")
    .Where("u.ativo", true)
    .OrderBy("u.nome")
    .Limit(10);

using (var mysql = new MySQL(config))
{
    await mysql.OpenAsync();
    using var reader = await mysql.ExecuteQueryAsync(builder);
    while (reader.Read())
    {
        Console.WriteLine($"{reader.GetString("nome")} - {reader.GetString("categoria")}");
    }
}
```

#### `Distinct`, `GroupBy` e `Having`

Para cenários de agregação, o builder agora suporta `Distinct()`, `GroupBy(...)`, `Having(...)`, `HavingRaw(...)`, `OrHaving(...)` e `OrHavingRaw(...)`.

```csharp
var builder = new SelectQueryBuilder()
    .Distinct()
    .Select("status")
    .SelectRaw("COUNT(*) AS total")
    .From("rastreamento_eventos")
    .Where("ativo", true, QueryOperator.Equals)
    .GroupBy("status")
    .Having("status", "cancelado", QueryOperator.NotEquals)
    .OrHavingRaw("COUNT(*) > {0}", 10)
    .OrderBy("status");
```

SQL gerada:

```sql
SELECT DISTINCT `status`, COUNT(*) AS total
FROM `rastreamento_eventos`
WHERE `ativo` = @p0
GROUP BY `status`
HAVING `status` <> @p1 OR COUNT(*) > @p2
ORDER BY `status` ASC
```

O `SelectQueryBuilder` também aceita `QueryOperator` em `Where(...)`, `OrWhere(...)`, `Having(...)` e `OrHaving(...)`, mantendo a mesma API tipada dos builders de `Update` e `Delete`.

#### Subqueries e `Exists`

O builder também suporta subqueries tipadas para evitar cair em `WhereRaw(...)` em cenários comuns:

```csharp
var eventosAtivos = new SelectQueryBuilder()
    .SelectRaw("1")
    .From("rastreamento_eventos re")
    .WhereRaw("re.veiculo_id = v.id")
    .Where("ativo", true);

var builder = new SelectQueryBuilder()
    .Select("v.id", "v.placa")
    .From("veiculos v")
    .WhereExists(eventosAtivos);
```

Também há overloads como:

```csharp
.WhereIn("v.id", subquery)
.WhereNotIn("v.id", subquery)
.WhereNotExists(subquery)
.OrWhereExists(subquery)
```

#### Paginação estruturada

Além de `Limit(...)`, você pode usar paginação estruturada com metadados:

```csharp
using var mysql = new MySQL(config);
await mysql.OpenAsync();

var page = await mysql.PaginateAsync<Veiculo>(
    SelectQueryBuilder.For<Veiculo>().Where("ativo", true),
    new PageRequest(page: 2, pageSize: 50));

Console.WriteLine(page.TotalItems);
Console.WriteLine(page.HasNextPage);
```

#### Projeção com `record` no `.Select(...)`

Você também pode usar um `record` ou DTO para definir automaticamente quais colunas devem entrar no `SELECT`.
Isso é útil para montar listas enxutas, evitar `SELECT *` e reaproveitar contratos já usados na aplicação.

```csharp
public record VehicleListItem(
    [property: JsonPropertyName("uuid")] string? Uuid,
    [property: JsonPropertyName("placa")] string? Placa,
    [property: JsonPropertyName("nome_portal")] string? NomePortal);

var builder = SelectQueryBuilder.For<Veiculo>()
    .Select<VehicleListItem>()
    .Where("cancelado", false)
    .Where("id_cliente", 12);
```

SQL gerada:

```sql
SELECT `uuid`, `placa`, `nome_portal` FROM `veiculo` WHERE `cancelado` = @p0 AND `id_cliente` = @p1
```

Formas suportadas:

```csharp
.Select<VehicleListItem>()
.Select(typeof(VehicleListItem))
.Select(new VehicleListItem(null, null, null))
```

Prioridade de resolução do nome da coluna no `.Select(...)` com `record`:

1. `DbField("...")`
2. `JsonPropertyName("...")`
3. Mapeamento do model base usado em `SelectQueryBuilder.For<T>()`
4. Fallback automático para `snake_case`

Exemplo com `DbField`:

```csharp
public record VehicleListItemCustom
{
    [DbField("nome_portal")]
    public string? NomePortal { get; init; }
}
```

Se não houver atributo nem mapeamento explícito, uma propriedade como `NomePortal` será convertida automaticamente para `nome_portal`.

#### Inspecionando a SQL antes de executar

Para obter a SQL parametrizada:

```csharp
var (sql, command) = builder.Build();
Console.WriteLine(sql);
```

Para obter uma versão pronta para debug/log, com os parâmetros substituídos:

```csharp
var debugSql = builder.ToDebugSql();
Console.WriteLine(debugSql);
```

Exemplo de saída:

```sql
SELECT `uuid`, `placa`, `nome_portal` FROM `veiculo` WHERE `cancelado` = 0 AND `id_cliente` = 12
```

`ToDebugSql()` é destinado a inspeção e logging. A execução real continua sendo feita com parâmetros, mantendo a proteção contra SQL Injection.

### Fluent Delete Query Builder

O `DeleteQueryBuilder` permite criar operações de exclusão de forma segura e fluente.

```csharp
// Delete com condições
var builder = new DeleteQueryBuilder()
    .Table("logs")
    .Where("created_at", DateTime.Now.AddDays(-30), QueryOperator.LessThan)
    .Limit(100);

using (var mysql = new MySQL(config))
{
    await mysql.OpenAsync();
    int deletedRows = await mysql.ExecuteDeleteAsync(builder);
}

// Mass Delete (Requer .All() explícito para segurança)
var massDelete = new DeleteQueryBuilder()
    .Table("temp_data")
    .All(); // Obrigatório se não houver WHERE

// Via DatabaseHelper
var helper = new DatabaseHelper(connectionString);
int rows = await helper.ExecuteDeleteAsync(builder);
```

#### Proteção contra Mass Delete
Por padrão, o `DeleteQueryBuilder` **impede** a execução de `DELETE` sem cláusula `WHERE`. Para deletar todas as linhas de uma tabela, você deve chamar explicitamente `.All()`:

```csharp
// ❌ Isso lançará InvalidOperationException
new DeleteQueryBuilder().Table("users").Build();

// ✅ Isso funciona
new DeleteQueryBuilder().Table("users").All().Build();
```

Se quiser endurecer ainda mais esse comportamento globalmente, use `MySQL.DefaultOptions.MutationProtection`:

```csharp
MySQL.DefaultOptions.MutationProtection.RequireConfirmationForAllOperations = true;
MySQL.DefaultOptions.MutationProtection.RequireLimitForDeleteAllOperations = true;

var builder = new DeleteQueryBuilder()
    .Table("logs")
    .All()
    .ConfirmMassOperation()
    .Limit(100);
```

O `UpdateQueryBuilder` também suporta `ConfirmMassOperation()` e `Limit(...)` para atualizações em massa controladas.

### QueryOperator Enum (Tipagem Forte)

Todos os builders (`Update`, `Select`, `Delete`) suportam o enum `QueryOperator` para evitar strings mágicas:

```csharp
using Jovemnf.MySQL.Builder;

var builder = new UpdateQueryBuilder()
    .Table("products")
    .Set("status", "discontinued")
    .Where("stock", 0, QueryOperator.Equals)
    .Where("last_sale", DateTime.Now.AddYears(-2), QueryOperator.LessThan);

// Operadores disponíveis:
// Equals, NotEquals, LessThan, LessThanOrEqual, GreaterThan, GreaterThanOrEqual
// Like, NotLike, IsNull, IsNotNull, In, NotIn, Between, Regexp, NotRegexp
```

<a id="mapeamento-e-orm"></a>
## Mapeamento e ORM

### Mapeamento Automático para Modelos (ORM)

O `MySQLReader` permite mapear os resultados diretamente para classes C# (POCOs) usando reflexão. O mapeador é inteligente: ele ignora maiúsculas/minúsculas e também remove underscores ao comparar nomes de colunas com propriedades (ex: mapeia automaticamente a coluna `tipo_pessoa` para a propriedade `TipoPessoa`).

```csharp
using Jovemnf.MySQL;
using Jovemnf.MySQL.Builder;

public class Usuario
{
    public int Id { get; set; }
    public string Nome { get; set; }
    public string Email { get; set; }
    public bool Ativo { get; set; }
    public DateTime DataCadastro { get; set; }
}

// ...

using (var mysql = new MySQL(config))
{
    await mysql.OpenAsync();
    var builder = new SelectQueryBuilder().Table("usuarios").Where("id", 1);
    
    using var reader = await mysql.ExecuteQueryAsync(builder);
    if (reader.Read())
    {
        // Mapeia uma única linha
        Usuario user = reader.ToModel<Usuario>();
    }
}

// Ou mapear uma lista completa:
List<Usuario> users = await reader.ToModelListAsync<Usuario>();
```

#### Conversores customizados

Você pode registrar conversores globais ou usar atributo por propriedade:

```csharp
public sealed class TrackerStatusConverter : IMySQLValueConverter
{
    public object ConvertFromDb(object value, Type targetType)
    {
        return string.Equals(value?.ToString(), "online", StringComparison.OrdinalIgnoreCase)
            ? TrackerStatus.Online
            : TrackerStatus.Offline;
    }
}

MySQLValueConverterRegistry.Register<TrackerStatus>(new TrackerStatusConverter());
```

Ou por atributo:

```csharp
public sealed class VeiculoView
{
    [DbConverter(typeof(UpperCaseStringConverter))]
    public string Placa { get; set; }
}
```

#### Ignorando propriedades na serialização para insert/update

Ao converter uma entidade para dicionário ou usar builders genéricos, você pode ignorar propriedades que não devem virar coluna:

```csharp
public sealed class PosicaoRastreador
{
    public int TrackerId { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }

    [IgnoreToDictionary]
    public string StatusCalculadoEmMemoria { get; set; }
}
```

Isso é útil para campos calculados na aplicação, dados transitórios e objetos auxiliares que não pertencem à tabela.

#### Multi-mapping

Para leituras de `JOIN` com dois modelos no mesmo resultado:

```csharp
using var mysql = new MySQL(config);
await mysql.OpenAsync();

var lista = await mysql.ExecuteQueryAsync<Usuario, Rastreador, UsuarioRastreadorView>(
    builder,
    (usuario, rastreador) => new UsuarioRastreadorView(usuario.Nome, rastreador.Codigo),
    splitOn: "tracker_id");
```

### ExecuteAsync&lt;T&gt; — Retorno tipado nos builders

Todos os builders fluentes oferecem um overload **`ExecuteAsync<T>(MySQL connection)`** que executa a operação e devolve o resultado já mapeado para o tipo `T`. O tipo `T` deve ter construtor sem parâmetros e propriedades compatíveis com as colunas (ou use os atributos `DbTable`/`DbField`).

| Builder | Retorno | Descrição |
|--------|--------|-----------|
| **SelectQueryBuilder** | `Task<List<T>>` | Todas as linhas do resultado mapeadas para `T`. |
| **InsertQueryBuilder** | `Task<T>` | A linha inserida (via `LAST_INSERT_ID()` + `SELECT`). Assume coluna de chave primária `id` auto-incremento. |
| **UpdateQueryBuilder** | `Task<T>` | A primeira linha afetada após o UPDATE (SELECT com o mesmo WHERE). Não suportado com `.All()`. |
| **DeleteQueryBuilder** | `Task<List<T>>` | As linhas que foram deletadas (SELECT com o mesmo WHERE antes do DELETE). Não suportado com `.All()`. |

#### Exemplos

**Select — lista tipada**
```csharp
using (var mysql = new MySQL(config))
{
    await mysql.OpenAsync();
    var lista = await SelectQueryBuilder.For<Usuario>()
        .Select("*")
        .Where("ativo", true)
        .ExecuteAsync<Usuario>(mysql);
    // lista é List<Usuario>
}
```

**Insert — entidade inserida**
```csharp
using (var mysql = new MySQL(config))
{
    await mysql.OpenAsync();
    var inserido = await InsertQueryBuilder.For<Usuario>()
        .Value("Nome", "Maria")
        .Value("Email", "maria@exemplo.com")
        .ExecuteAsync<Usuario>(mysql);
    // inserido contém os dados do banco (incluindo Id gerado)
}
```

**Update — entidade atualizada**
```csharp
using (var mysql = new MySQL(config))
{
    await mysql.OpenAsync();
    var atualizado = await UpdateQueryBuilder.For<Usuario>()
        .Set("Nome", "Maria Silva")
        .Where("Id", 123)
        .ExecuteAsync<Usuario>(mysql);
    // atualizado é a linha já atualizada ou default se 0 linhas
}
```

**Delete — entidades removidas**
```csharp
using (var mysql = new MySQL(config))
{
    await mysql.OpenAsync();
    var removidas = await DeleteQueryBuilder.For<Usuario>()
        .Where("status", "excluido")
        .ExecuteAsync<Usuario>(mysql);
    // removidas é List<Usuario> com as linhas que foram deletadas
}
```

Quando não precisar do resultado tipado, use o `ExecuteAsync(connection)` sem tipo (ou os métodos existentes na classe `MySQL`) para obter apenas o número de linhas afetadas ou o reader.

### 🏷️ Mapeamento Avançado (Atributos)

Você pode usar os atributos `DbTable` e `DbField` para definir explicitamente os nomes das tabelas e colunas, permitindo que os Builders resolvam esses nomes automaticamente.

```csharp
[DbTable("usuarios_v2")] // Define o nome da tabela no DB
public class Usuario
{
    [DbField("user_id")] // Define o nome da coluna no DB
    public int Id { get; set; }

    [DbField("full_name")]
    public string NomeCompleto { get; set; }
}
```

#### Builders Genéricos

Ao usar a versão genérica dos Builders, o nome da tabela e os mapeamentos de campos são resolvidos automaticamente:

```csharp
// Nome da tabela "usuarios_v2" e coluna "user_id" resolvidos automaticamente via atributos
var sql = SelectQueryBuilder.For<Usuario>()
    .Select("Id", "NomeCompleto") // Mapeia para user_id, full_name
    .Where("Id", 1)              // Mapeia para user_id
    .OrderBy("NomeCompleto")      // Mapeia para full_name
    .ToString();

// Suporte a snake_case
// Você pode usar a versão snake_case da propriedade mesmo sem o atributo
var sql2 = SelectQueryBuilder.For<Usuario>()
    .Where("nome_completo", "Maria") // Resolve para a coluna full_name
    .ToString();
```

Isso funciona para todos os builders:
- `SelectQueryBuilder<T>`
- `InsertQueryBuilder<T>`
- `InsertBatchQueryBuilder<T>`
- `UpdateQueryBuilder<T>`
- `DeleteQueryBuilder<T>`

#### Exemplo com Insert e Update
```csharp
var user = new Usuario { Id = 1, NomeCompleto = "João Silva" };

// Insert
var insert = new InsertQueryBuilder<Usuario>()
    .Values(user.ToDictionary()) // Mapeia propriedades para colunas automaticamente
    .ToString();

// Update
var update = new UpdateQueryBuilder<Usuario>()
    .Set("NomeCompleto", "João Modificado")
    .Where("Id", user.Id)
    .ToString();
```

<a id="operacoes-avancadas"></a>
## Operações Avançadas

### Execução com Resultados Detalhados

Use o `UpdateQueryExecutor` para obter informações detalhadas sobre a execução:

```csharp
var executor = new UpdateQueryExecutor(connection);
var result = await executor.ExecuteWithResultAsync(builder);

if (result.Success)
{
    Console.WriteLine($"Linhas afetadas: {result.RowsAffected}");
    Console.WriteLine($"Tempo de execução: {result.ExecutionTime.TotalMilliseconds}ms");
}
else
{
    Console.WriteLine($"Erro: {result.Error}");
}
```

### CancellationToken, transação por escopo e observabilidade

As principais APIs assíncronas agora possuem overload com `CancellationToken`, permitindo cancelamento cooperativo em jobs, APIs HTTP e rotinas de monitoramento:

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
var reader = await mysql.ExecuteQueryAsync(builder, cts.Token);
```

Para transações por escopo:

```csharp
await mysql.WithTransactionAsync(async db =>
{
    await db.ExecuteUpdateAsync(updateBuilder, cancellationToken);
    await db.ExecuteInsertAsync(insertBuilder, lastId: false, cancellationToken);
}, cancellationToken);
```

E para observabilidade com `ILogger`:

```csharp
var options = new MySQLOptions
{
    Logger = logger,
    SqlMasker = sql => sql.Replace("senha", "***")
};

using var mysql = new MySQL(config, options);
```

Quando configurado, o logger recebe operação, tempo de execução, presença de transação, linhas afetadas e SQL para debug já mascarada.

#### Configurando `MySQLOptions`

As opções avançadas ficam centralizadas em `MySQLOptions`:

```csharp
var options = new MySQLOptions
{
    Logger = logger,
    SqlMasker = sql => sql.Replace("token", "***"),
    MutationProtection = new MySQLMutationProtectionOptions
    {
        RequireConfirmationForAllOperations = true,
        RequireLimitForDeleteAllOperations = true,
        RequireLimitForUpdateAllOperations = true
    },
    Bulk = new MySQLBulkOptions
    {
        DefaultChunkSize = 500
    }
};
```

Você pode aplicar essas opções por instância:

```csharp
using var mysql = new MySQL(config, options);
```

Ou num fluxo com `DatabaseHelper`:

```csharp
var helper = new DatabaseHelper(connectionString, options);
```

#### ExecuteScalar, health check e execução em lote

Além dos builders, a classe `MySQL` também expõe utilitários diretos para cenários operacionais:

```csharp
using var mysql = new MySQL(config, options);
await mysql.OpenAsync();

var versao = await mysql.ExecuteScalarAsync("SELECT VERSION()", cancellationToken);
bool conectado = await mysql.TestConnectionAsync(cancellationToken);

int total = await mysql.ExecuteBatchAsync(
    cancellationToken,
    "UPDATE veiculos SET ativo = 1 WHERE tracker_id = 10",
    "UPDATE veiculos SET ultima_leitura = NOW() WHERE tracker_id = 10");
```

Casos de uso típicos:
- `ExecuteScalar*`: métricas, contagens rápidas e leitura de valores únicos.
- `TestConnection*`: health checks de APIs, workers e serviços internos.
- `ExecuteBatch*`: rotinas administrativas e scripts curtos executados na mesma transação.

<a id="databasehelper"></a>
## DatabaseHelper

`DatabaseHelper` é útil quando você quer executar builders sem gerenciar a instância de `MySQL` manualmente.

Exemplo básico:

```csharp
var helper = new DatabaseHelper(connectionString);

int rows = await helper.ExecuteUpdateAsync(
    new UpdateQueryBuilder()
        .Table("veiculos")
        .Set("status", "online")
        .Where("tracker_id", 10));
```

Versão avançada com `MySQLOptions` e `CancellationToken`:

```csharp
var helper = new DatabaseHelper(connectionString, options);

var pagina = await helper.PaginateAsync<Veiculo>(
    SelectQueryBuilder.For<Veiculo>().Where("ativo", true),
    new PageRequest(1, 100),
    cancellationToken);

await helper.WithTransactionAsync(async db =>
{
    await db.ExecuteUpdateAsync(updateBuilder, cancellationToken);
    await db.ExecuteInsertAsync(insertBuilder, lastId: false, cancellationToken);
}, cancellationToken);
```

O helper também oferece atalhos legados para cenários específicos:

```csharp
int atualizado = await helper.ExecuteUpdateWithTransactionAsync(async (connection, transaction) =>
{
    return new UpdateQueryBuilder()
        .Table("rastreador_evento")
        .Set("processado", true)
        .Where("id", 123);
});

List<int> resultados = await helper.ExecuteMultipleUpdatesAsync(builder1, builder2, builder3);
```

<a id="suporte-espacial"></a>
## Suporte Espacial

Para sistemas de rastreamento veicular, o pacote também inclui tipos e helpers geométricos em `Jovemnf.MySQL.Geometry`.

### `Point`

```csharp
using Jovemnf.MySQL.Geometry;

var posicao = new Point(-23.5505, -46.6333); // latitude, longitude
byte[] wkb = posicao.ToWKB();
```

### `Polygon`

```csharp
var cerca = new Polygon(new List<Point>
{
    new(-23.5500, -46.6340),
    new(-23.5500, -46.6320),
    new(-23.5510, -46.6320),
    new(-23.5510, -46.6340)
});

string wkt = cerca.ToWKT();
```

### Helpers geométricos

```csharp
double distancia = origem.DistanceTo(destino);
bool dentroDaCerca = posicao.IsInside(cerca);
var pontosProximos = origem.PointsWithinRadius(listaDePontos, 500);
var circulo = origem.CreateCircle(300);
var boundingBox = origem.CreateBoundingBox(1000, 800);
```

Esses helpers são úteis para geofencing, proximidade, agrupamento e validações espaciais antes mesmo da persistência no banco.

<a id="testes"></a>
### Testes

O projeto inclui uma suíte de testes robustos focada em funcionalidade e segurança (SQL Injection).

Para rodar os testes:
1. Restaure os pacotes do projeto.
2. Execute:

```bash
dotnet test MysqlTest/MysqlTest.csproj
```

Os testes validarão:
- **Segurança:** Proteção contra SQL Injection em Tabelas, Colunas e Valores.
- **Builders:** Geração correta de queries complexas (Joins, WhereIn, Between).
- **Mapeamento:** Lógica de conversão de dados e nomes de colunas.

<a id="troubleshooting"></a>
### Troubleshooting (Resolução de Problemas)

**Erro: `NETSDK1004: Arquivo de ativos project.assets.json não encontrado`**
Se você limpar o projeto ou clonar o repositório e ver este erro:
1. Clique com o botão direito na **Solution** no Rider.
2. Selecione **Restore NuGet Packages**.
3. Aguarde o término e tente compilar novamente.

<a id="seguranca"></a>
## 🔒 Segurança

O pacote `Jovemnf.MySQL` prioriza a segurança dos seus dados:

1.  **Parametrização Automática:** Todos os valores passados aos Query Builders são automaticamente tratados como parâmetros SQL, prevenindo injeção nos dados.
2.  **Escape de Identificadores:** Nomes de tabelas e campos são escapados (backticks) para evitar injeção em nomes de colunas.
3.  **Whitelist de Operadores:** A construção de queries aceita apenas uma lista pré-definida de operadores válidos (`=`, `<>`, `LIKE`, `IN`, `REGEXP`, etc.), impedindo a inserção de comandos maliciosos.
4.  **Proteção contra Mass Operations:** `UpdateQueryBuilder` e `DeleteQueryBuilder` **bloqueiam** operações sem cláusula `WHERE` por padrão. Para executar atualizações ou exclusões em massa, você deve chamar explicitamente `.All()`, garantindo que essas operações perigosas sejam intencionais.

```csharp
// ❌ Isso lançará InvalidOperationException
new UpdateQueryBuilder().Table("users").Set("active", false).Build();

// ✅ Isso funciona (mass update intencional)
new UpdateQueryBuilder().Table("users").Set("active", false).All().Build();
```

<a id="licenca"></a>
## 📄 Licença

Este projeto está sob a licença [MIT](LICENSE).


