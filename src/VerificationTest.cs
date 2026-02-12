using System;
using System.Collections.Generic;
using Jovemnf.MySQL;

namespace Jovemnf.MySQL.Tests;

public class Program
{
    public static void Main()
    {
        TestIdentifierEscaping();
        TestOperatorWhitelisting();
        Console.WriteLine("\nTodos os testes passaram!");
    }

    public static void TestIdentifierEscaping()
    {
        Console.WriteLine("Testando escape de identificadores...");
        
        var builder = new UpdateQueryBuilder()
            .Table("users`; DROP TABLE users; --")
            .Set("name`", "João")
            .Where("id`", 1);

        var (sql, command) = builder.Build();

        Console.WriteLine($"SQL Gerado: {sql}");

        // Verificar se os backticks foram duplicados e se o SQL está protegido
        if (sql.Contains("`users``; DROP TABLE users; --`") && 
            sql.Contains("`name``` = @p0") && 
            sql.Contains("`id``` = @p1"))
        {
            Console.WriteLine("✅ Escape de identificadores funciona!");
        }
        else
        {
            throw new Exception("❌ Falha no escape de identificadores!");
        }
    }

    public static void TestOperatorWhitelisting()
    {
        Console.WriteLine("\nTestando whitelist de operadores...");

        try
        {
            new UpdateQueryBuilder()
                .Table("users")
                .Set("name", "João")
                .Where("id", 1, "= 1; DROP TABLE users; --");
            
            throw new Exception("❌ Falha: Operador malicioso foi aceito!");
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"✅ Operador inválido bloqueado: {ex.Message}");
        }

        try
        {
            var builder = new UpdateQueryBuilder()
                .Table("users")
                .Set("name", "João")
                .Where("age", 18, ">=");
            
            var (sql, _) = builder.Build();
            if (sql.Contains(">= @p1"))
            {
                Console.WriteLine("✅ Operador válido aceito!");
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"❌ Falha: Operador válido foi rejeitado! {ex.Message}");
        }
    }
}
