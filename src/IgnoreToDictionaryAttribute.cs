using System;

namespace Jovemnf.MySQL;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public sealed class IgnoreToDictionaryAttribute : Attribute
{
}
