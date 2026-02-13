using System;

namespace Jovemnf.MySQL;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public sealed class DbFieldAttribute : Attribute
{
    public string Name { get; }
    public DbFieldAttribute(string name) => Name = name;
}
