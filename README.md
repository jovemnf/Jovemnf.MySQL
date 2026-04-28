# Jovemnf.MySQL

Pacote .NET Core de alto desempenho para interação simplificada com bancos de dados MySQL.

## Sumário

- [Instalação](#instalacao)
- [Configuração de conexão](#configuracao)
- [Sharding (Múltiplas Conexões)](#sharding)
- [Builders fluentes](#builders-fluentes)
- [Mapeamento e ORM](#mapeamento-e-orm)
- [Entidades tipadas (Active Record)](#entidades-tipadas)
- [Streaming de resultados](#streaming)
- [Operações avançadas](#operacoes-avancadas)
- [Paginação](#paginacao)
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
- **Entidades tipadas (Active Record):** classe base `Entity<TSelf>` que expõe atalhos estáticos estilo LINQ diretamente na entidade (`Veiculo.Where(v => v.IdVeiculo == 1)`, `Veiculo.FindAsync(...)`, `Veiculo.Update()`, etc.).
- **Chaves Primárias:** atributo `[DbPrimaryKey]` com suporte a **PKs compostas** (via `Order`) e busca pronta via `Entity<TSelf>.FindByPkAsync(...)`.
- **Streaming (`IAsyncEnumerable<T>`):** leitura linha-a-linha para resultados grandes sem pico de memória (`await foreach` em `StreamAsync<T>`/`ToModelStreamAsync<T>`).
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

<a id="sharding"></a>
### Sharding / Múltiplas Conexões

O pacote suporta de forma nativa o gerenciamento de múltiplos shards (bancos de dados ou instâncias diferentes) utilizando um gerenciador global. Você pode registrar várias configurações associando-as a uma **Tag** (que pode ser `string`, `int`, `enum` ou qualquer `object`) e recuperá-las dinamicamente.

#### Configuração básica

No `Startup.cs` ou `Program.cs`:

```csharp
using Jovemnf.MySQL;
using Jovemnf.MySQL.Configuration;

// Shard 1 (ex: usando int como tag)
MySQL.GlobalShards.AddShard(new MySQLConfiguration
{
    Tag = 1,
    Host = "db-shard-01.local",
    Database = "cliente_abc",
    Username = "user",
    Password = "pwd"
});

// Shard 2 (ex: usando string como tag)
MySQL.GlobalShards.AddShard(new MySQLConfiguration
{
    Tag = "Relatorios",
    Host = "db-readonly.local",
    Database = "analytics",
    Username = "user",
    Password = "pwd",
    IsDefault = true // Será usado caso nenhuma tag seja informada
});
```

#### Pool de conexão por Shard

Cada shard pode ter sua própria configuração de pool, garantindo isolamento total entre conexões de diferentes bancos. Basta informar a propriedade `Pool`:

```csharp
// Shard de LEITURA: precisa de muitas conexões
MySQL.GlobalShards.AddShard(new MySQLConfiguration
{
    Tag = "Leitura",
    Host = "db-readonly.local",
    Database = "analytics",
    Username = "user",
    Password = "pwd",
    Pool = new PoolConfiguration
    {
        MaxPoolSize = 200,
        MinPoolSize = 20,
        ConnectionTimeout = 10
    }
});

// Shard de ESCRITA: pool menor e mais controlado
MySQL.GlobalShards.AddShard(new MySQLConfiguration
{
    Tag = "Escrita",
    Host = "db-master.local",
    Database = "principal",
    Username = "user",
    Password = "pwd",
    IsDefault = true,
    Pool = new PoolConfiguration
    {
        MaxPoolSize = 50,
        MinPoolSize = 5,
        ConnectionTimeout = 30,
        IdleTimeout = 180,
        KeepaliveInterval = 30
    }
});
```

Se `Pool` não for informado, os valores globais (propriedades estáticas da classe `MySQL`) são usados automaticamente, mantendo retrocompatibilidade.

> **Nota:** O MySqlConnector já garante isolamento de pool por connection string. Cada shard com host, database ou credenciais diferentes **nunca** compartilha conexões com outro shard, independentemente da configuração de pool.

#### Uso simplificado

Para abrir a conexão desejada, basta invocar `MySQL.FromShard(...)` passando a Tag:

```csharp
// Conectando no shard de leitura (pool: max=200, min=20)
var dbLeitura = MySQL.FromShard("Leitura");
await dbLeitura.OpenAsync();

// Conectando no shard de escrita (pool: max=50, min=5)
var dbEscrita = MySQL.FromShard("Escrita");
await dbEscrita.OpenAsync();

// Conectando no banco default (usa o shard com IsDefault = true)
var dbPadrao = MySQL.FromShard();
await dbPadrao.OpenAsync();

// Tags dinâmicas: int, enum ou qualquer object
var db1 = MySQL.FromShard(1);
var dbEnum = MySQL.FromShard(MinhaEnum.TenantA);
```

#### Gerenciamento de Shards

```csharp
// Verificar se há shards configurados
bool temShards = MySQL.GlobalShards.HasShards;

// Buscar um shard sem lançar exceção
if (MySQL.GlobalShards.TryGetShard("Leitura", out var config))
{
    Console.WriteLine($"Host: {config.Host}");
}

// Listar todos os shards
foreach (var shard in MySQL.GlobalShards.GetAllShards())
{
    Console.WriteLine($"Tag: {shard.Tag}, Host: {shard.Host}");
}

// Remover um shard
MySQL.GlobalShards.RemoveShard("Leitura");
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

#### `Where` tipado com expressão

Nos builders genéricos, você também pode escrever o `WHERE` de forma mais próxima de LINQ:

```csharp
var builder = SelectQueryBuilder.For<Veiculo>()
    .Where(v => v.IdCliente == 12 && v.Ativo && v.Status != "bloqueado");
```

Nesse caso, o parâmetro `v` é do tipo do model informado em `For<T>()`. Isso permite escrever filtros com autocomplete e segurança de compilação sobre as propriedades da entidade.

Exemplos suportados:

```csharp
SelectQueryBuilder.For<Veiculo>()
    .Where(v => v.IdCliente == 12 && v.Ativo);

SelectQueryBuilder.For<Veiculo>()
    .Where(v => !v.Ativo || v.Status == null);

var ids = new[] { 10, 12, 15 };
SelectQueryBuilder.For<Veiculo>()
    .Where(v => ids.Contains(v.IdCliente));

SelectQueryBuilder.For<Veiculo>()
    .Where(v => ids.Any(id => id == v.IdCliente));

SelectQueryBuilder.For<Veiculo>()
    .Where(v => v.Status.Contains("bloq") || v.Status.StartsWith("off"));

SelectQueryBuilder.For<Veiculo>()
    .Where(v => !ids.Contains(v.IdCliente) && !v.Status.EndsWith("ado"));

SelectQueryBuilder.For<Veiculo>()
    .Where(v => v.Status != null && v.Status.Contains("bloq"));

SelectQueryBuilder.For<Veiculo>()
    .Where(v => (v.IdCliente == 12 || v.Ativo) && v.Status != "bloqueado");
```

#### SQL gerada pelas expressões

Exemplo:

```csharp
SelectQueryBuilder.For<Veiculo>()
    .Where(v => v.Status.Contains("bloq") || v.Status.StartsWith("off"));
```

SQL parametrizada:

```sql
SELECT * FROM `veiculo` WHERE `status` LIKE @p0 OR `status` LIKE @p1
```

Parâmetros:

```csharp
@p0 = "%bloq%"
@p1 = "off%"
```

Versão de debug:

```sql
SELECT * FROM `veiculo` WHERE `status` LIKE '%bloq%' OR `status` LIKE 'off%'
```

Outros exemplos úteis:

```csharp
SelectQueryBuilder.For<Veiculo>()
    .Where(v => ids.Contains(v.IdCliente));
```

```sql
SELECT * FROM `veiculo` WHERE `id_cliente` IN (@p0, @p1, @p2)
```

```csharp
SelectQueryBuilder.For<Veiculo>()
    .Where(v => ids.Any(id => id == v.IdCliente));
```

```sql
SELECT * FROM `veiculo` WHERE `id_cliente` IN (@p0, @p1, @p2)
```

Quando o predicate do `Any` não é uma igualdade simples, o builder expande para `OR` parametrizado:

```csharp
var ids = new[] { 10, 20 };

SelectQueryBuilder.For<Veiculo>()
    .Where(v => ids.Any(id => id > v.IdCliente));
```

```sql
SELECT * FROM `veiculo` WHERE `id_cliente` < @p0 OR `id_cliente` < @p1
```

```csharp
SelectQueryBuilder.For<Veiculo>()
    .Where(v => !ids.Any(id => id > v.IdCliente));
```

```sql
SELECT * FROM `veiculo` WHERE NOT (`id_cliente` < @p0 OR `id_cliente` < @p1)
```

Casos especiais:

- coleção vazia em `Any(...)` gera `WHERE 1 = 0`
- coleção vazia em `!Any(...)` gera `WHERE 1 = 1`

```csharp
SelectQueryBuilder.For<Veiculo>()
    .Where(v => !ids.Contains(v.IdCliente) && !v.Status.EndsWith("ado"));
```

```sql
SELECT * FROM `veiculo` WHERE `id_cliente` NOT IN (@p0, @p1, @p2) AND `status` NOT LIKE @p3
```

```csharp
SelectQueryBuilder.For<Veiculo>()
    .Where(v => v.Status != null && v.Status.Contains("bloq"));
```

```sql
SELECT * FROM `veiculo` WHERE `status` IS NOT NULL AND `status` LIKE @p0
```

```csharp
SelectQueryBuilder.For<Veiculo>()
    .Where(v => (v.IdCliente == 12 || v.Ativo) && v.Status != "bloqueado");
```

```sql
SELECT * FROM `veiculo` WHERE (`id_cliente` = @p0 OR `ativo` = @p1) AND `status` <> @p2
```

Regras de tradução aplicadas:

- `campo.Contains("x")` -> `LIKE '%x%'`
- `campo.StartsWith("x")` -> `LIKE 'x%'`
- `campo.EndsWith("x")` -> `LIKE '%x'`
- `lista.Contains(campo)` -> `IN (...)`
- `!lista.Contains(campo)` -> `NOT IN (...)`
- `lista.Any(x => x == campo)` -> `IN (...)`
- `!lista.Any(x => x == campo)` -> `NOT IN (...)`
- `campo != null` -> `IS NOT NULL`
- `campo == null` -> `IS NULL`
- parênteses na expressão são preservados na SQL gerada

Observação:

- os nomes da tabela e das colunas continuam respeitando `DbTable`, `DbField` e o fallback automático para `snake_case`
- a execução real continua parametrizada; a versão "de debug" serve apenas para inspeção

Hoje, essa tradução cobre bem:

- comparações com `==`, `!=`, `>`, `>=`, `<`, `<=`
- booleano direto, como `v.Ativo`
- negação booleana, como `!v.Ativo`
- comparações com `null`
- `lista.Contains(v.Campo)` para gerar `IN (...)`
- `!lista.Contains(v.Campo)` para gerar `NOT IN (...)`
- `v.Campo.Contains("x")`, `StartsWith("x")` e `EndsWith("x")` para gerar `LIKE`
- null-guard, como `v.Campo != null && v.Campo.Contains("x")`
- composição com `&&` e `||`
- agrupamentos com parênteses, como `(a || b) && c`

O mesmo estilo também funciona nos builders genéricos de update e delete:

```csharp
UpdateQueryBuilder.For<Veiculo>()
    .Where(v => v.IdCliente >= 10 && v.Status != "offline")
    .Set("Status", "online");

DeleteQueryBuilder.For<Veiculo>()
    .Where(v => v.IdCliente == 99 || !v.Ativo);
```

Limitações atuais:

- chamadas arbitrárias de método fora dos padrões suportados (`Contains`, `StartsWith`, `EndsWith`) ainda não são traduzidas automaticamente
- o parser ainda não tenta converter expressões mais avançadas com subqueries, `Any`, `All`, agregações ou navegação entre objetos
- quando a expressão sair do subconjunto suportado, prefira os métodos fluentes tradicionais (`Where`, `OrWhere`, `WhereRaw`, etc.)

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

Além de `Limit(...)`, você pode usar paginação estruturada com metadados de navegação através de `PageRequest` e `PagedResult<T>`. Veja a seção [Paginação](#paginacao) para detalhes completos.

```csharp
using var mysql = new MySQL(config);
await mysql.OpenAsync();

var page = await SelectQueryBuilder.For<Veiculo>()
    .Where("ativo", true)
    .PaginateAsync<Veiculo>(mysql, new PageRequest(page: 2, pageSize: 50));

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

<a id="entidades-tipadas"></a>
## Entidades tipadas (Active Record)

A classe abstrata `Entity<TSelf>` usa o padrão **CRTP** (Curiously Recurring Template Pattern) para expor atalhos estáticos tipados diretamente no próprio modelo, no estilo LINQ/Active Record. A ideia é que você escreva menos código de plumbing e consiga fazer consultas como:

```csharp
var builder = Veiculo.Where(v => v.IdVeiculo == 1);
var ativos  = await Veiculo.Where(v => v.Ativo).ExecuteAsync<Veiculo>(mysql);
```

Por baixo dos panos, `Entity<TSelf>` apenas orquestra os builders já existentes (`SelectQueryBuilder<T>`, `UpdateQueryBuilder<T>`, `DeleteQueryBuilder<T>`, `InsertQueryBuilder<T>`, `InsertBatchQueryBuilder<T>`), respeitando o mapeamento definido pelos atributos `DbTable`, `DbField` e `IgnoreToDictionary`.

### Definindo a entidade

Basta herdar de `Entity<TSelf>` referenciando a si mesma (CRTP) e aplicar os atributos de mapeamento normalmente:

```csharp
using Jovemnf.MySQL;

[DbTable("veiculos")]
public sealed class Veiculo : Entity<Veiculo>
{
    [DbField("id_veiculo")]
    public int IdVeiculo { get; set; }

    [DbField("id_cliente")]
    public int IdCliente { get; set; }

    public string Placa { get; set; } = null!;

    public bool Ativo { get; set; }

    public string Status { get; set; } = null!;

    [DbField("data_cadastro")]
    public DateTime DataCadastro { get; set; }

    [IgnoreToDictionary]
    public string StatusCalculadoEmMemoria { get; set; } = "";
}
```

> ⚠️ A restrição `where TSelf : Entity<TSelf>, new()` exige que a entidade tenha construtor sem parâmetros — padrão de praticamente todos os POCOs mapeados pela lib.

### Construindo consultas com `Where` tipado

O `Where(Expression<Func<TSelf, bool>>)` aproveita o tradutor de expressões da lib, incluindo suporte a `&&`, `||`, `!`, `==`, `!=`, `<`, `<=`, `>`, `>=`, comparação com `null`, `string.Contains/StartsWith/EndsWith`, `collection.Contains(v.Campo)` e `collection.Any(x => x == v.Campo)`.

```csharp
// Simples
var porId = Veiculo.Where(v => v.IdVeiculo == 1);

// Composto (AND/OR)
var filtrados = Veiculo.Where(v =>
    v.IdCliente == 10
    && v.Ativo
    && v.Status != "bloqueado");

// IN/NOT IN a partir de coleção
var ids = new[] { 10, 12, 15 };
var porLista = Veiculo.Where(v => ids.Contains(v.IdVeiculo));

// LIKE automático
var porPlaca = Veiculo.Where(v => v.Placa.StartsWith("ABC"));

// NULL/NOT NULL
var semStatus = Veiculo.Where(v => v.Status == null);
```

O SQL gerado é totalmente parametrizado (nada de concatenação) e os nomes das colunas são resolvidos via `DbField` (ex.: `v.IdVeiculo` → `` `id_veiculo` ``).

### Encadeando como LINQ

O retorno de `Where`/`Query`/`Select`/`OrderBy`/`Limit` é um `SelectQueryBuilder<TSelf>`, então você pode continuar encadeando com toda a API do builder:

```csharp
var pagina = Veiculo
    .Where(v => v.IdCliente == 10 && v.Ativo)
    .OrderBy(nameof(Veiculo.DataCadastro), "DESC")
    .Limit(20, offset: 0);

var (sql, command) = pagina.Build();
// SELECT * FROM `veiculos`
// WHERE `id_cliente` = @p0 AND `ativo` = @p1
// ORDER BY `data_cadastro` DESC LIMIT 20
```

### Executando a consulta

Os builders aceitam tanto uma instância aberta de `MySQL` quanto um `DatabaseHelper` (que gerencia a conexão internamente):

```csharp
// Usando MySQL já aberto
await using var mysql = new MySQL(config);
await mysql.OpenAsync();

List<Veiculo> ativos = await Veiculo
    .Where(v => v.Ativo)
    .OrderBy("placa")
    .ExecuteAsync<Veiculo>(mysql);

// Usando DatabaseHelper (abre/fecha automaticamente)
var helper = new DatabaseHelper(connectionString);

List<Veiculo> porCliente = await helper.ExecuteQueryAsync<Veiculo>(
    Veiculo.Where(v => v.IdCliente == 42));
```

### Atalhos de execução direta

Para consultas simples você pode pular o builder intermediário:

```csharp
// Todos os registros
var todos = await Veiculo.AllAsync(mysql);

// Filtro direto
var bloqueados = await Veiculo.FindAsync(v => v.Status == "bloqueado", mysql);

// Existência
bool existe = await Veiculo.ExistsAsync(v => v.IdVeiculo == 1, mysql);

// Contagem
long total   = await Veiculo.CountAsync(mysql);
long ativos  = await Veiculo.CountAsync(v => v.Ativo, mysql);

// Também funciona com DatabaseHelper:
var usando = await Veiculo.AllAsync(helper);
var achados = await Veiculo.FindAsync(v => v.IdCliente == 10, helper);
```

### Mutações (Update / Delete / Insert / InsertBatch)

O `Entity<TSelf>` também expõe atalhos para os demais builders, mantendo tipagem forte:

```csharp
// UPDATE: Veiculo.Update() -> UpdateQueryBuilder<Veiculo>
var updates = await Veiculo.Update()
    .Set(nameof(Veiculo.Status), "ativo")
    .Set(nameof(Veiculo.Ativo), true)
    .Where(v => v.IdCliente == 10 && v.Status == "pendente")
    .ExecuteAsync(mysql);

// DELETE: Veiculo.Delete() -> DeleteQueryBuilder<Veiculo>
await Veiculo.Delete()
    .Where(v => !v.Ativo && v.DataCadastro < DateTime.Now.AddYears(-5))
    .ExecuteAsync(mysql);

// INSERT: Veiculo.Insert() -> InsertQueryBuilder<Veiculo>
// Retorna a entidade já hidratada com o id gerado pelo banco.
var novo = await Veiculo.Insert()
    .ValuesFrom(new Veiculo
    {
        IdCliente   = 42,
        Placa       = "ABC1D23",
        Ativo       = true,
        Status      = "ativo",
        DataCadastro = DateTime.UtcNow
    })
    .ExecuteAsync<Veiculo>(mysql);

Console.WriteLine($"Novo veículo id={novo.IdVeiculo}");

// INSERT em lote: Veiculo.InsertBatch() -> InsertBatchQueryBuilder<Veiculo>
var inseridos = await Veiculo.InsertBatch()
    .RowsFrom(listaDeVeiculos)
    .ExecuteAsync(mysql);
```

### Seleção de colunas específicas

`Select(params string[])` restringe os campos retornados (útil para performance):

```csharp
var resumo = await Veiculo
    .Select(nameof(Veiculo.IdVeiculo), nameof(Veiculo.Placa), nameof(Veiculo.Ativo))
    .Where(v => v.IdCliente == 10)
    .ExecuteAsync<Veiculo>(mysql);

// SELECT `id_veiculo`, `placa`, `ativo` FROM `veiculos` WHERE `id_cliente` = @p0
```

### Integração com paginação

Como `Entity<TSelf>.Where(...)` devolve um `SelectQueryBuilder<TSelf>`, toda a API de paginação continua funcionando:

```csharp
var pagina = await Veiculo
    .Where(v => v.Ativo)
    .OrderBy(nameof(Veiculo.DataCadastro), "DESC")
    .PaginateAsync<Veiculo>(mysql, new PageRequest { Page = 1, PageSize = 20 });

Console.WriteLine($"Total: {pagina.TotalCount}, Páginas: {pagina.TotalPages}");
foreach (var v in pagina.Items)
{
    Console.WriteLine(v.Placa);
}
```

### Chave primária com `[DbPrimaryKey]` e `FindByPkAsync`

Marque as colunas que compõem a chave primária com o atributo `[DbPrimaryKey]` e ganhe de graça os atalhos `FindByPkAsync` e `ExistsByPkAsync`. O atributo suporta **chaves primárias compostas** via a propriedade `Order`, que define a posição de cada coluna.

#### Chave primária simples

```csharp
[DbTable("veiculos")]
public sealed class Veiculo : Entity<Veiculo>
{
    [DbPrimaryKey]
    [DbField("id_veiculo")]
    public int IdVeiculo { get; set; }

    public string Placa { get; set; } = null!;
}

// Uso:
Veiculo? v = await Veiculo.FindByPkAsync(mysql, 42);
bool existe = await Veiculo.ExistsByPkAsync(mysql, 42);

// Também funciona com DatabaseHelper:
Veiculo? v2 = await Veiculo.FindByPkAsync(helper, 42);
```

SQL gerado:

```sql
SELECT * FROM `veiculos` WHERE `id_veiculo` = @p0 LIMIT 1
```

#### Chave primária composta

Use `Order` para definir a ordem em que os valores serão passados no `FindByPkAsync`:

```csharp
[DbTable("usuarios_permissoes")]
public sealed class UsuarioPermissao : Entity<UsuarioPermissao>
{
    [DbPrimaryKey(Order = 0)]
    [DbField("id_usuario")]
    public int IdUsuario { get; set; }

    [DbPrimaryKey(Order = 1)]
    [DbField("id_permissao")]
    public int IdPermissao { get; set; }

    public bool Ativo { get; set; }
}

// Valores posicionais (na ordem do Order):
var rel = await UsuarioPermissao.FindByPkAsync(mysql, idUsuario: 10, idPermissao: 5);
// SELECT * FROM `usuarios_permissoes`
// WHERE `id_usuario` = @p0 AND `id_permissao` = @p1 LIMIT 1

// Ou por dicionário (aceita nome da coluna ou da propriedade, case-insensitive):
var rel2 = await UsuarioPermissao.FindByPkAsync(mysql, new Dictionary<string, object>
{
    ["id_usuario"]  = 10,
    ["id_permissao"] = 5,
});

// Verificação de existência:
bool temRel = await UsuarioPermissao.ExistsByPkAsync(mysql, 10, 5);
```

#### Diagnóstico e introspecção

A classe expõe propriedades estáticas úteis para debug e uso genérico:

```csharp
IReadOnlyList<string> cols = UsuarioPermissao.PrimaryKeyColumns;
// ["id_usuario", "id_permissao"]

bool composta = UsuarioPermissao.HasCompositePrimaryKey;  // true
```

#### Validações e mensagens de erro

O `FindByPkAsync` aplica validações claras antes de tocar no banco:

| Situação | Exceção | Mensagem |
| -------- | ------- | -------- |
| Entidade sem `[DbPrimaryKey]` | `InvalidOperationException` | Orienta a marcar a propriedade com `[DbPrimaryKey]`. |
| Quantidade de valores diferente do nº de colunas de PK | `ArgumentException` | Lista as colunas de PK esperadas e quantas foram informadas. |
| Dicionário com chave desconhecida / coluna não coberta | `ArgumentException` | Indica qual coluna/propriedade ficou sem valor. |

> 💡 Dica: se você criar uma entidade sem PK e tentar `FindByPkAsync`, a exceção acontece imediatamente (sem abrir conexão com o banco), então é fácil identificar em testes.

### API estática disponível em `Entity<TSelf>`

| Método | Retorno | Descrição |
| ------ | ------- | --------- |
| `Query()` | `SelectQueryBuilder<TSelf>` | Builder cru, já apontando para a tabela correta. |
| `Where(Expression<Func<TSelf,bool>>)` | `SelectQueryBuilder<TSelf>` | `WHERE` tipado estilo LINQ. |
| `Where(string field, object value, string op = "=")` | `SelectQueryBuilder<TSelf>` | `WHERE` campo/valor clássico. |
| `Select(params string[])` | `SelectQueryBuilder<TSelf>` | Restringe colunas retornadas. |
| `OrderBy(string field, string direction = "ASC")` | `SelectQueryBuilder<TSelf>` | Ordenação. |
| `Limit(int limit, int offset = 0)` | `SelectQueryBuilder<TSelf>` | `LIMIT`/`OFFSET`. |
| `Update()` / `Delete()` / `Insert()` / `InsertBatch()` | Builders tipados | Atalhos para mutação. |
| `AllAsync(mysql \| helper)` | `Task<List<TSelf>>` | Retorna todas as linhas. |
| `StreamAllAsync(mysql \| helper[, ct])` | `IAsyncEnumerable<TSelf>` | Itera todas as linhas sem carregar tudo em memória. |
| `StreamAsync(predicate, mysql \| helper[, ct])` | `IAsyncEnumerable<TSelf>` | Itera linhas que satisfazem o predicado em modo streaming. |
| `FindAsync(predicate, mysql \| helper)` | `Task<List<TSelf>>` | Busca tipada. |
| `FindByPkAsync(mysql \| helper, params object[])` | `Task<TSelf>` | Busca por chave primária (simples ou composta). Retorna `null` se não encontrado. |
| `FindByPkAsync(mysql \| helper, IReadOnlyDictionary<string,object>)` | `Task<TSelf>` | Busca por PK informando valores por nome de coluna/propriedade. |
| `ExistsByPkAsync(mysql, params object[])` | `Task<bool>` | Verifica existência por chave primária. |
| `ExistsAsync(predicate, mysql[, ct])` | `Task<bool>` | `EXISTS (SELECT 1 ... LIMIT 1)`. |
| `CountAsync([predicate,] mysql)` | `Task<long>` | `COUNT(*)` com ou sem filtro. |
| `PrimaryKeyColumns` | `IReadOnlyList<string>` | Colunas de PK ordenadas pelo `Order`. |
| `HasCompositePrimaryKey` | `bool` | `true` quando a PK envolve 2+ colunas. |

### Por que usar `Entity<TSelf>`

- **Ergonomia:** `Veiculo.Where(v => v.Id == 1)` é mais legível que `SelectQueryBuilder.For<Veiculo>().Where(v => v.Id == 1)` e não acopla o código de domínio ao nome do builder.
- **Reaproveitamento total:** nada é reescrito — os atalhos apenas delegam para os builders existentes, preservando segurança, parametrização, validações e testes já existentes.
- **Tipagem forte:** o compilador garante que `v.IdVeiculo` existe, evitando strings mágicas em filtros recorrentes.
- **Migração suave:** você pode adotar gradualmente. Modelos que não herdam de `Entity<TSelf>` continuam funcionando via `SelectQueryBuilder.For<T>()` normalmente.

<a id="streaming"></a>
## Streaming de resultados (`IAsyncEnumerable<T>`)

Para resultados grandes — tabelas com milhares ou milhões de linhas — materializar tudo em uma `List<T>` pode consumir memória demais. O pacote expõe uma API de **streaming** baseada em `IAsyncEnumerable<T>` que:

- Lê linha por linha direto do cursor do MySQL (o driver `MySqlConnector` já é streaming por natureza).
- Mapeia cada linha para o modelo sob demanda dentro do `await foreach`.
- **Nunca mantém a lista completa em memória**.
- Fecha automaticamente o reader (e a conexão, quando aberta pela lib) ao final do loop, com `break` ou em caso de exceção.

### Onde está disponível

| Nível | Método | Observação |
| ----- | ------ | ---------- |
| `MySQLReader` | `ToModelStreamAsync<T>(ct)` | Mais baixo nível: assume que você já tem um reader aberto. |
| `MySQL` | `ExecuteQueryStreamAsync<T>(builder, ct)` | Cuida do reader; conexão já deve estar aberta. |
| `DatabaseHelper` | `ExecuteQueryStreamAsync<T>(builder, ct)` | Abre/fecha conexão automaticamente durante a enumeração. |
| `SelectQueryBuilder` | `StreamAsync<T>(mysql\|helper, ct)` | Encadeamento fluente. |
| `Entity<TSelf>` | `StreamAllAsync(...)` / `StreamAsync(predicate, ...)` | Active-record. |

### Exemplo básico com `Entity<TSelf>`

```csharp
await using var mysql = new MySQL(config);
await mysql.OpenAsync();

// Itera sem carregar tudo em memória.
await foreach (var v in Veiculo.StreamAsync(v => v.Ativo, mysql))
{
    ProcessarVeiculo(v);
    // Cada iteração consome ~1 linha — útil para exportações grandes, ETL, etc.
}
```

### Com `SelectQueryBuilder` (acesso completo ao builder)

```csharp
var builder = Veiculo.Query()
    .OrderBy(nameof(Veiculo.IdVeiculo))
    .Where(v => v.IdCliente == 42);

await foreach (var v in builder.StreamAsync<Veiculo>(mysql, cancellationToken))
{
    Console.WriteLine(v.Placa);
}
```

### Com `DatabaseHelper` (conexão gerenciada)

```csharp
var helper = new DatabaseHelper(connectionString);

await foreach (var v in Veiculo.StreamAllAsync(helper, cancellationToken))
{
    // A conexão é aberta no início e fechada ao terminar o foreach.
    // Evite I/O demorado dentro do loop para não segurar conexão do pool.
}
```

### Com reader pré-existente

```csharp
await using var reader = await mysql.ExecuteQueryAsync(builder, cancellationToken);

await foreach (var v in reader.ToModelStreamAsync<Veiculo>(cancellationToken))
{
    // Use quando você precisa controlar o reader manualmente (ex.: múltiplos result sets).
}
```

### Processamento em lote (chunking) enquanto consome o stream

Quando você precisa acumular em "pacotes" (por exemplo, para gravar em outro sistema em lotes de 1.000), combine o streaming com um buffer simples:

```csharp
const int tamanhoDoLote = 1_000;
var lote = new List<Veiculo>(tamanhoDoLote);

await foreach (var v in Veiculo.StreamAsync(v => v.Ativo, mysql, ct))
{
    lote.Add(v);
    if (lote.Count == tamanhoDoLote)
    {
        await EnviarParaOutroSistemaAsync(lote, ct);
        lote.Clear();
    }
}

if (lote.Count > 0)
    await EnviarParaOutroSistemaAsync(lote, ct);
```

### Exemplo 1: Exportação de posições para CSV (milhões de linhas)

Cenário típico: exportar **todas as posições GPS** de um período grande para um arquivo CSV sem estourar a memória do servidor.

```csharp
public async Task ExportarPosicoesCsvAsync(
    DateTime inicio,
    DateTime fim,
    string caminhoArquivo,
    CancellationToken ct = default)
{
    await using var writer = new StreamWriter(caminhoArquivo);
    await writer.WriteLineAsync("id_posicao,id_rastreador,lat,lng,velocidade,data_hora");

    var builder = PosicaoRastreador.Query()
        .Where(p => p.DataHora >= inicio && p.DataHora <= fim)
        .OrderBy(nameof(PosicaoRastreador.DataHora));

    long total = 0;
    await foreach (var p in builder.StreamAsync<PosicaoRastreador>(helper, ct))
    {
        await writer.WriteLineAsync(
            $"{p.IdPosicao},{p.IdRastreador},{p.Latitude},{p.Longitude},{p.Velocidade},{p.DataHora:o}");
        total++;
    }

    Console.WriteLine($"Exportadas {total:N0} posições para {caminhoArquivo}.");
}
```

Diferença com `ExecuteAsync<T>`: uma consulta de 5 milhões de posições que consumiria vários GB de RAM passa a rodar com uso de memória praticamente constante.

### Exemplo 2: Serialização JSON nativa em streaming

O `System.Text.Json` aceita `IAsyncEnumerable<T>` diretamente em `SerializeAsync`, então é possível gerar um JSON array gigante sem buffer intermediário:

```csharp
public async Task ExportarVeiculosJsonAsync(
    Stream output,
    CancellationToken ct = default)
{
    var stream = Veiculo.StreamAllAsync(helper, ct);

    // O JsonSerializer consome o IAsyncEnumerable e escreve diretamente no output.
    await JsonSerializer.SerializeAsync(output, stream, new JsonSerializerOptions
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    }, ct);
}

// Uso: gerar um arquivo JSON de 2GB sem encher a RAM
await using var fs = File.Create("veiculos.json");
await ExportarVeiculosJsonAsync(fs);
```

### Exemplo 3: ETL — migrar dados entre bancos em fluxo

Copiar milhões de linhas de uma origem para outra, usando `InsertBatch` com chunks:

```csharp
public async Task MigrarVeiculosAsync(
    DatabaseHelper origem,
    DatabaseHelper destino,
    CancellationToken ct = default)
{
    const int loteSize = 500;
    var buffer = new List<Veiculo>(loteSize);
    long totalInseridos = 0;

    await foreach (var v in Veiculo.StreamAllAsync(origem, ct))
    {
        buffer.Add(v);

        if (buffer.Count == loteSize)
        {
            await using var mysqlDestino = new MySQL(/* connection string destino */);
            await mysqlDestino.OpenAsync(ct);

            var inseridos = await Veiculo.InsertBatch()
                .RowsFrom(buffer)
                .ExecuteAsync(mysqlDestino, ct);

            totalInseridos += inseridos;
            buffer.Clear();
        }
    }

    // Resto
    if (buffer.Count > 0)
    {
        await using var mysqlDestino = new MySQL(/* connection string destino */);
        await mysqlDestino.OpenAsync(ct);
        totalInseridos += await Veiculo.InsertBatch().RowsFrom(buffer).ExecuteAsync(mysqlDestino, ct);
    }

    Console.WriteLine($"Migrados {totalInseridos:N0} veículos.");
}
```

> Dica: use **duas** conexões distintas (origem e destino) em vez de reutilizar a mesma — o cursor de leitura do `StreamAllAsync` está segurando uma conexão enquanto o loop está ativo.

### Exemplo 4: Agregações sem carregar o resultado inteiro

Calcular estatísticas (distância total percorrida, velocidade média, quantidade de eventos) sem nunca materializar a lista:

```csharp
public async Task<EstatisticasVeiculo> CalcularEstatisticasAsync(
    int idVeiculo,
    DateTime inicio,
    DateTime fim,
    CancellationToken ct = default)
{
    long quantidade = 0;
    double velocidadeTotal = 0;
    double velocidadeMaxima = 0;
    double distanciaTotal = 0;

    PosicaoRastreador anterior = null;

    var builder = PosicaoRastreador.Query()
        .Where(p => p.IdVeiculo == idVeiculo
                 && p.DataHora >= inicio
                 && p.DataHora <= fim)
        .OrderBy(nameof(PosicaoRastreador.DataHora));

    await foreach (var p in builder.StreamAsync<PosicaoRastreador>(helper, ct))
    {
        quantidade++;
        velocidadeTotal += p.Velocidade;
        if (p.Velocidade > velocidadeMaxima) velocidadeMaxima = p.Velocidade;

        if (anterior != null)
            distanciaTotal += Haversine(anterior.Latitude, anterior.Longitude, p.Latitude, p.Longitude);

        anterior = p;
    }

    return new EstatisticasVeiculo
    {
        QuantidadePosicoes = quantidade,
        VelocidadeMedia    = quantidade > 0 ? velocidadeTotal / quantidade : 0,
        VelocidadeMaxima   = velocidadeMaxima,
        DistanciaKmTotal   = distanciaTotal,
    };
}
```

### Exemplo 5: Envio para API externa com `HttpClient` em lotes

```csharp
public async Task SincronizarComApiExternaAsync(
    HttpClient http,
    CancellationToken ct = default)
{
    const int tamanhoLote = 100;
    var lote = new List<Veiculo>(tamanhoLote);

    await foreach (var v in Veiculo.StreamAsync(v => v.PendenteSincronizacao, helper, ct))
    {
        lote.Add(v);
        if (lote.Count == tamanhoLote)
        {
            await EnviarLoteAsync(http, lote, ct);
            lote.Clear();
        }
    }

    if (lote.Count > 0)
        await EnviarLoteAsync(http, lote, ct);

    static async Task EnviarLoteAsync(HttpClient http, List<Veiculo> lote, CancellationToken ct)
    {
        var resp = await http.PostAsJsonAsync("/api/v1/veiculos/sync", lote, ct);
        resp.EnsureSuccessStatusCode();
    }
}
```

### Exemplo 6: Paralelismo controlado sobre o stream

Para processar itens em paralelo sem explodir a memória (cada item entra em um "worker" assim que sai do stream), use `Parallel.ForEachAsync` com o stream diretamente:

```csharp
await Parallel.ForEachAsync(
    Veiculo.StreamAllAsync(helper, ct),
    new ParallelOptions { MaxDegreeOfParallelism = 8, CancellationToken = ct },
    async (veiculo, innerCt) =>
    {
        await ProcessarAsync(veiculo, innerCt);
    });
```

> Atenção: `Parallel.ForEachAsync` mantém o cursor aberto enquanto itera. Evite operações síncronas bloqueantes dentro do delegate.

### Exemplo 7: Projeção no cliente (post-processamento)

Quando você quer transformar cada item antes de consumir, combine o stream com um `Select` assíncrono simples. O ideal é fazer a projeção direto no SQL (via `Select(...)`), mas caso precise de lógica em C#:

```csharp
async IAsyncEnumerable<string> PlacasFormatadasAsync([EnumeratorCancellation] CancellationToken ct = default)
{
    await foreach (var v in Veiculo.StreamAllAsync(helper, ct))
    {
        yield return $"{v.Placa} ({v.Modelo})";
    }
}

await foreach (var linha in PlacasFormatadasAsync(ct))
{
    Console.WriteLine(linha);
}
```

### Exemplo 8: Early-exit com `break` (procurar e parar)

Diferente de `ExecuteAsync<T>`, que só retorna depois de ler tudo, o stream para **imediatamente** quando você sai do loop:

```csharp
// Encontra a primeira posição suspeita (velocidade > 180) e para de ler o banco na hora
PosicaoRastreador suspeita = null;

await foreach (var p in PosicaoRastreador.StreamAsync(p => p.IdVeiculo == 42, helper, ct))
{
    if (p.Velocidade > 180)
    {
        suspeita = p;
        break; // o reader é fechado aqui; o resto da tabela nem é lido.
    }
}
```

### Cancelamento cooperativo

Todos os métodos aceitam `CancellationToken`. Por se tratar de `IAsyncEnumerable`, você também pode usar o operador `WithCancellation`:

```csharp
await foreach (var v in Veiculo
    .StreamAllAsync(mysql)
    .WithCancellation(cancellationToken))
{
    // ...
}
```

### Comparação "lista" vs "stream" na prática

O exemplo abaixo ilustra o impacto real quando você precisa processar uma tabela com **1 milhão** de linhas:

```csharp
// ❌ Carrega TODAS as linhas em memória antes de processar
//    Pico de memória ~ tamanho_medio_da_linha * 1_000_000
List<PosicaoRastreador> todas = await PosicaoRastreador.AllAsync(helper, ct);
foreach (var p in todas)
{
    await ProcessarAsync(p, ct);
}
// ≈ centenas de MB ou GB de RAM, dependendo do modelo.

// ✅ Processa uma linha por vez, memória praticamente constante
await foreach (var p in PosicaoRastreador.StreamAllAsync(helper, ct))
{
    await ProcessarAsync(p, ct);
}
// ≈ alguns KB de RAM por iteração (só a linha corrente).
```

O tempo total de banco tende a ser **o mesmo** (ou até melhor no stream, porque você começa a processar a primeira linha muito antes). O que muda drasticamente é o **pico de memória** e o **tempo até o primeiro item** (TTFB).

### Quando usar streaming vs. lista

| Situação | Recomendação |
| -------- | ------------ |
| Resultado cabe tranquilo em memória (< algumas dezenas de milhares de linhas) | `ExecuteAsync<T>` / `AllAsync` (List em memória) |
| Resultado grande ou desconhecido, processamento pode ser feito "em fluxo" | `StreamAsync<T>` / `StreamAllAsync` |
| Exportação para arquivo (CSV/JSON), integração com outro sistema, ETL | `StreamAsync<T>` |
| Precisa de acesso aleatório ou múltiplas passadas | `ExecuteAsync<T>` (ou cache próprio do consumidor) |
| Precisa do primeiro item o mais rápido possível (latência baixa) | `StreamAsync<T>` + `break` após encontrar |

### Considerações importantes

- **Tempo de vida da conexão:** enquanto o `await foreach` está ativo, o reader (e, no caso do `DatabaseHelper`, a conexão do pool) fica ocupado. Evite bloqueios longos dentro do loop.
- **LINQ em cima do stream:** você pode usar `System.Linq.Async` (pacote NuGet separado) para `Select`, `Where`, `Take` etc. sobre `IAsyncEnumerable<T>`, mas nenhum método sai da máquina cliente — filtros de banco devem continuar sendo feitos no `SelectQueryBuilder`.
- **Não cachear a enumeração:** se precisar iterar duas vezes, reexecute o stream (ele não é reutilizável).

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

<a id="paginacao"></a>
## Paginação

O pacote oferece paginação estruturada com metadados de navegação através de dois tipos simples definidos em `Jovemnf.MySQL`:

### `PageRequest`

Representa a requisição de uma página. É um `readonly struct` com validação no construtor:

```csharp
public readonly struct PageRequest(int page, int pageSize)
{
    public int Page { get; }       // número da página (>= 1)
    public int PageSize { get; }   // itens por página (>= 1)
    public int Offset => (Page - 1) * PageSize; // offset calculado automaticamente
}
```

Uso:

```csharp
var request = new PageRequest(page: 1, pageSize: 50);
// request.Offset -> 0

var request2 = new PageRequest(3, 20);
// request2.Offset -> 40
```

> Passar `page < 1` ou `pageSize < 1` lança `ArgumentOutOfRangeException`.

### `PagedResult<T>`

Representa o resultado paginado com os itens e todos os metadados de navegação prontos:

```csharp
public sealed class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; }
    public long TotalItems { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; }       // ceil(TotalItems / PageSize)
    public bool HasNextPage { get; }     // Page < TotalPages
    public bool HasPreviousPage { get; } // Page > 1
}
```

### Executando via `MySQL`

Você pode encadear `PaginateAsync<T>` direto no `SelectQueryBuilder`, no mesmo estilo do `ExecuteAsync<T>`:

```csharp
using Jovemnf.MySQL;
using Jovemnf.MySQL.Builder;

using var mysql = new MySQL(config);
await mysql.OpenAsync();

PagedResult<Veiculo> pagina = await SelectQueryBuilder.For<Veiculo>()
    .Where("ativo", true)
    .OrderBy("placa")
    .PaginateAsync<Veiculo>(mysql, new PageRequest(page: 2, pageSize: 50));

Console.WriteLine($"Página {pagina.Page} de {pagina.TotalPages}");
Console.WriteLine($"Total de itens: {pagina.TotalItems}");
Console.WriteLine($"Tem próxima? {pagina.HasNextPage}");
Console.WriteLine($"Tem anterior? {pagina.HasPreviousPage}");

foreach (var veiculo in pagina.Items)
{
    Console.WriteLine(veiculo.Placa);
}
```

Se preferir, a forma tradicional (chamando a partir do `MySQL`) continua funcionando:

```csharp
var builder = SelectQueryBuilder.For<Veiculo>()
    .Where("ativo", true)
    .OrderBy("placa");

PagedResult<Veiculo> pagina = await mysql.PaginateAsync<Veiculo>(
    builder,
    new PageRequest(page: 2, pageSize: 50));
```

### Executando via `DatabaseHelper`

Quando não quiser gerenciar a instância de `MySQL` manualmente, também dá para encadear no builder:

```csharp
var helper = new DatabaseHelper(connectionString);

var pagina = await SelectQueryBuilder.For<Veiculo>()
    .Where("ativo", true)
    .OrderBy("placa")
    .PaginateAsync<Veiculo>(helper, new PageRequest(1, 100), cancellationToken);
```

Ou na forma tradicional:

```csharp
var pagina = await helper.PaginateAsync<Veiculo>(
    SelectQueryBuilder.For<Veiculo>().Where("ativo", true),
    new PageRequest(1, 100),
    cancellationToken);
```

### Exemplo prático em uma API

```csharp
[HttpGet("veiculos")]
public async Task<IActionResult> Listar(int page = 1, int pageSize = 20, CancellationToken ct = default)
{
    var builder = SelectQueryBuilder.For<Veiculo>()
        .Where("id_cliente", ClienteAtualId)
        .OrderBy("placa");

    var pagina = await _helper.PaginateAsync<Veiculo>(
        builder,
        new PageRequest(page, pageSize),
        ct);

    return Ok(new
    {
        items = pagina.Items,
        page = pagina.Page,
        pageSize = pagina.PageSize,
        totalItems = pagina.TotalItems,
        totalPages = pagina.TotalPages,
        hasNextPage = pagina.HasNextPage,
        hasPreviousPage = pagina.HasPreviousPage
    });
}
```

### Como funciona internamente

- O `PaginateAsync<T>` executa duas queries na mesma conexão:
  1. Um `SELECT COUNT(*)` derivado do seu `SelectQueryBuilder` para obter `TotalItems`.
  2. O `SELECT` original com `LIMIT {PageSize} OFFSET {Offset}` para obter `Items`.
- Os filtros (`Where`, `WhereIn`, `Join`, etc.) e o `OrderBy` do builder são respeitados.
- Defina um `OrderBy` estável (ex: chave primária) para garantir ordem previsível entre páginas.

### Dicas

- Use `PageSize` com limites razoáveis (ex: 10, 20, 50, 100) para evitar resultados gigantes.
- Combine com `Select<T>()` para projetar apenas as colunas necessárias no listing:

```csharp
var builder = SelectQueryBuilder.For<Veiculo>()
    .Select<VehicleListItem>()
    .Where("cancelado", false);

var pagina = await mysql.PaginateAsync<VehicleListItem>(
    builder,
    new PageRequest(1, 50));
```

- Em endpoints HTTP, passe sempre o `CancellationToken` da requisição para permitir cancelamento cooperativo.

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


