namespace gAPI.Core.Attributes;

[AttributeUsage(AttributeTargets.Interface)]
public class GenerateBusAttribute : Attribute
{
    public GenerateBusAttribute()
    {
    }
    public GenerateBusAttribute(string apiName)
    {
        Name = apiName;
    }

    public string? Name { get; }
}