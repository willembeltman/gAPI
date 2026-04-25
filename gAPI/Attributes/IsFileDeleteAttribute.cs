using System;

namespace gAPI.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class IsFileDeleteAttribute(Type updateType) : Attribute
{
    public Type UpdateType { get; } = updateType;
}