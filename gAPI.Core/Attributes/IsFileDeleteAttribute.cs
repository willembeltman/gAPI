using System;

namespace gAPI.Core.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class IsFileDeleteAttribute(Type updateType) : Attribute
{
    public Type UpdateType { get; } = updateType;
}