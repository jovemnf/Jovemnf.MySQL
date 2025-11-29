# Testes Automatizados - Jovemnf.MySQL

Este projeto contém os testes automatizados para a biblioteca Jovemnf.MySQL.

## Estrutura dos Testes

Os testes estão organizados nos seguintes arquivos:

- **MySQLTests.cs**: Testes para a classe principal `MySQL`, incluindo:
  - Criação de instâncias com diferentes construtores
  - Configuração de conexão
  - Execução de comandos
  - Gerenciamento de recursos (Dispose)

- **MySQLReaderTests.cs**: Testes para a classe `MySQLReader`, incluindo:
  - Leitura de dados de diferentes tipos
  - Tratamento de erros
  - Métodos assíncronos

- **MySQLCriteriaTests.cs**: Testes para a classe `MySQLCriteria`, incluindo:
  - Adição de colunas
  - Uso de aliases
  - Formatação de queries

- **TryParseTests.cs**: Testes para a classe interna `TryParse`, incluindo:
  - Conversão para diferentes tipos de dados
  - Tratamento de erros de conversão

- **ExceptionTests.cs**: Testes para as exceções customizadas:
  - `MySQLConnectException`
  - `MySQLCloseException`

## Como Executar os Testes

### Usando dotnet CLI

```bash
# Executar todos os testes
dotnet test

# Executar testes com output detalhado
dotnet test --verbosity normal

# Executar testes específicos
dotnet test --filter "FullyQualifiedName~MySQLTests"
```

### Usando Visual Studio

1. Abra a solução `Jovemnf.MySQL.sln`
2. No Test Explorer, clique em "Run All" ou execute testes individuais

### Usando Visual Studio Code

1. Instale a extensão .NET Core Test Explorer
2. Execute os testes através da interface do Test Explorer

## Dependências

Os testes utilizam as seguintes bibliotecas:

- **xUnit**: Framework de testes
- **Moq**: Biblioteca para criação de mocks
- **MySqlConnector**: Driver MySQL (mesma dependência da biblioteca principal)

## Observações

- Alguns testes podem falhar se não houver um banco de dados MySQL configurado. Esses testes são principalmente para validação de estrutura e lógica básica.
- Para testes de integração completos, seria necessário configurar um banco de dados de teste.
- Os testes utilizam mocks quando possível para evitar dependências externas.

## Cobertura de Testes

Os testes cobrem:

- ✅ Criação e configuração de conexões MySQL
- ✅ Execução de comandos SQL
- ✅ Leitura de dados (MySQLReader)
- ✅ Construção de queries (MySQLCriteria)
- ✅ Conversão de tipos (TryParse)
- ✅ Tratamento de exceções
- ✅ Gerenciamento de recursos (Dispose)
