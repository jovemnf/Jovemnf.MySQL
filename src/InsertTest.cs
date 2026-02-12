using System;
using System.Collections.Generic;
using Jovemnf.MySQL;

namespace Jovemnf.MySQL.Tests;

public class InsertTest
{
    public static void Main()
    {
        TestInsertSqlGeneration();
        TestInsertIdentifierEscaping();
        Console.WriteLine("\nTodos os testes de INSERT passaram!");
    }

    public static void TestInsertSqlGeneration()
    {
        Console.WriteLine("Testando geração de SQL para INSERT...");
        
        var builder = new InsertQueryBuilder()
            .Table("usuarios")
            .Value("nome", "Teste")
            .Value("email", "teste@exemplo.com");

        var (sql, command) = builder.Build();

        Console.WriteLine($"SQL Gerado: {sql}");

        if (sql.Contains("INSERT INTO `usuarios` (`nome`, `email`) VALUES (@p0, @p1)"))
        {
            Console.WriteLine("✅ Geração de SQL básica funciona!");
        }
        else
        {
            throw new Exception("❌ Falha na geração do SQL básico!");
        }
    }

    public static void TestInsertIdentifierEscaping()
    {
        Console.WriteLine("\nTestando escape de identificadores no INSERT...");
        
        var builder = new InsertQueryBuilder()
            .Table("users`--")
            .Value("field`", "value");

        var (sql, _) = builder.Build();

        Console.WriteLine($"SQL Gerado: {sql}");

        if (sql.Contains("INSERT INTO `users``--` (`field```) VALUES (@p0)"))
        {
            Console.WriteLine("✅ Escape de identificadores funciona!");
        }
        else
        {
            throw new Exception("❌ Falha no escape de identificadores!");
        }
    }
}
