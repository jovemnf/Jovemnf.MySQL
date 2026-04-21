using System;

namespace Jovemnf.MySQL;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
public sealed class DbTableAttribute : Attribute
{
    public string Name { get; }
    public DbTableAttribute(string name) => Name = name;
}
