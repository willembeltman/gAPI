using System;

namespace gAPI.Attributes;

public class IsListNotByAttribute : Attribute
{
    public IsListNotByAttribute(string foreignKeyName, Type foreignType) { }
}
