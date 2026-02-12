using System;
using System.Collections.Generic;
using Jovemnf.MySQL;

namespace Jovemnf.MySQL.Tests;

public class SelectTest
{
    public static void Main()
    {
        TestSimpleSelect();
        TestComplexSelect();
        TestSelectIdentifierEscaping();
        Console.WriteLine("\nTodos os testes de SELECT passaram!");
    }

    public static void TestSimpleSelect()
    {
        Console.WriteLine("Testando geração de SQL para SELECT simples...");
        
        var builder = new SelectQueryBuilder()
            .Select("id", "nome")
            .From("usuarios")
            .Where("ativo", true);

        var (sql, _) = builder.Build();

        Console.WriteLine($"SQL Gerado: {sql}");

        if (sql.Contains("SELECT `id`, `nome` FROM `usuarios` WHERE `ativo` = @p0"))
        {
            Console.WriteLine("✅ Geração de SQL básica funciona!");
        }
        else
        {
            throw new Exception("❌ Falha na geração do SQL básico!");
        }
    }

    public static void TestComplexSelect()
    {
        Console.WriteLine("\nTestando geração de SQL para SELECT complexo (Join, OrderBy, Limit)...");
        
        var builder = new SelectQueryBuilder()
            .Select("u.nome", "c.nome as categoria")
            .From("usuarios u")
            .Join("categorias c", "u.categoria_id", "=", "c.id")
            .Where("u.id", 10, ">")
            .OrderBy("u.nome", "DESC")
            .Limit(5, 10);

        var (sql, _) = builder.Build();

        Console.WriteLine($"SQL Gerado: {sql}");

        if (sql.Contains("SELECT `u`.`nome`, `c`.`nome` as categoria FROM `usuarios` `u` INNER JOIN `categorias` `c` ON `u`.`categoria_id` = `c`.`id` WHERE `u`.`id` > @p0 ORDER BY `u`.`nome` DESC LIMIT 5 OFFSET 10"))
        {
            Console.WriteLine("✅ Geração de SQL complexo funciona!");
        }
        else
        {
            throw new Exception("❌ Falha na geração do SQL complexo!");
        }
    }

    public static void TestSelectIdentifierEscaping()
    {
        Console.WriteLine("\nTestando escape de identificadores no SELECT...");
        
        var builder = new SelectQueryBuilder()
            .Select("field`", "table.col`")
            .From("table`--");

        var (sql, _) = builder.Build();

        Console.WriteLine($"SQL Gerado: {sql}");

        if (sql.Contains("SELECT `field```, `table`.`col``` FROM `table``--`"))
        {
            Console.WriteLine("✅ Escape de identificadores funciona!");
        }
        else
        {
            throw new Exception("❌ Falha no escape de identificadores!");
        }
    }
}
