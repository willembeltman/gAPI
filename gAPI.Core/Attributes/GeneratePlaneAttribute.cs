namespace gAPI.Core.Attributes;

[AttributeUsage(AttributeTargets.Interface)]
public class GeneratePlaneAttribute : Attribute
{
    public GeneratePlaneAttribute()
    {
    }
    public GeneratePlaneAttribute(string apiName)
    {
        Name = apiName;
    }

    public string? Name { get; }
}