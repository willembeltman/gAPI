using System;

namespace gAPI.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class IsStateManagedAttribute(string stateProperty, bool checkForNull = false, bool useValue = false, bool isString = false) : Attribute
{
    public string Name { get; } = stateProperty;
    public bool CheckForNull { get; } = checkForNull;
    public bool UseValue { get; } = useValue;
    public bool IsString { get; } = isString;
}