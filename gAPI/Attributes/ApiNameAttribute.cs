using System;

namespace gAPI.Attributes;

public class ApiNameAttribute : Attribute
{
    public ApiNameAttribute(string name)
    {
        Name = name;
    }
    public string? Name { get; }
}
