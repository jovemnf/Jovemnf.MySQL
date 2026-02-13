# Jovemnf.MySQL

Pacote .NET Core de alto desempenho para interação simplificada com bancos de dados MySQL.

## ✨ Características

- **Gerenciamento de Conexão:** Fácil configuração via `MySQLConfiguration`.
- **Query Builders Fluentes:** APIs intuitivas para `SELECT`, `INSERT`, `UPDATE` e `DELETE`.
- **Tipagem Forte:** Enum `QueryOperator` para evitar strings mágicas em operadores SQL.
- **Segurança Nativa:** Proteção robusta contra SQL Injection (parametrização, escape de identificadores e proteção contra mass operations).
- **Mapeamento Automático (ORM):** Conversão automática de resultados para classes C# (POCOs).
- **Suporte a Transações:** Execução atômica de múltiplas operações.
- **Async/Await:** Suporte completo para operações assíncronas.

## 🚀 Instalação

Adicione o pacote ao seu projeto:

```bash
dotnet add package Jovemnf.MySQL
```

## 📖 Como Usar

### Configuração de Conexão

Você pode configurar a conexão usando o objeto `MySQLConfiguration`:

```csharp
using Jovemnf.MySQL;

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
MySQL.INIT(config);
```

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

### Testes

O projeto inclui uma suíte de testes robustos focada em funcionalidade e segurança (SQL Injection).

Para rodar os testes:
1. Abra o projeto no **JetBrains Rider** ou **Visual Studio**.
2. Navegue até o arquivo `src/TestRunner.cs`.
3. Clique com o botão direito no método `Main` e selecione **Run**.

Os testes validarão:
- **Segurança:** Proteção contra SQL Injection em Tabelas, Colunas e Valores.
- **Builders:** Geração correta de queries complexas (Joins, WhereIn, Between).
- **Mapeamento:** Lógica de conversão de dados e nomes de colunas.

### Troubleshooting (Resolução de Problemas)

**Erro: `NETSDK1004: Arquivo de ativos project.assets.json não encontrado`**
Se você limpar o projeto ou clonar o repositório e ver este erro:
1. Clique com o botão direito na **Solution** no Rider.
2. Selecione **Restore NuGet Packages**.
3. Aguarde o término e tente compilar novamente.

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

## 📄 Licença

Este projeto está sob a licença [MIT](LICENSE).


