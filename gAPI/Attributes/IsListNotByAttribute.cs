using System;

namespace gAPI.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class IsListNotByAttribute(string foreignKeyName, Type foreignType) : Attribute
{
    public string ForeignKeyName { get; } = foreignKeyName;
    public Type ForeignType { get; } = foreignType;
}
