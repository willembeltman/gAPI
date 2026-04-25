using System;

namespace gAPI.Attributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public class IsForeignKeyAttribute(Type type) : Attribute
{
    public Type Type { get; } = type
            ?? throw new ArgumentNullException(nameof(type), "Type cannot be null.");
}
