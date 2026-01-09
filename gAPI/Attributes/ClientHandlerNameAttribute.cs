using System;

namespace gAPI.Attributes;

public class ClientHandlerNameAttribute : Attribute
{
    public ClientHandlerNameAttribute(string name)
    {
        Name = name;
    }
    public string? Name { get; }
}
