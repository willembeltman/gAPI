namespace gAPI.Core.Attributes;

[AttributeUsage(AttributeTargets.Interface)]
public class GenerateHubAttribute : Attribute
{
    public GenerateHubAttribute()
    {
    }
    public GenerateHubAttribute(string apiName)
    {
        Name = apiName;
    }

    public string? Name { get; }
}