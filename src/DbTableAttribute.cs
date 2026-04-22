using System;

namespace Jovemnf.MySQL;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
public sealed class DbTableAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}
