namespace gAPI.Core.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class IsForeignNameAttribute(string foreignKeyName) : Attribute
{
    public string ForeignKeyName { get; } = foreignKeyName;
}