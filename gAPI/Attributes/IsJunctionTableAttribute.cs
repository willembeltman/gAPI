using System;

namespace gAPI.Attributes;

[AttributeUsage(AttributeTargets.Interface)]
public class IsJunctionTableAttribute(Type typeLeft, Type typeRight) : Attribute
{
    public Type TypeLeft { get; } = typeLeft;
    public Type TypeRight { get; } = typeRight;
}