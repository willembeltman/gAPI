namespace gAPI.Attributes;

[AttributeUsage(AttributeTargets.Interface)]
public class GenerateMinimalApiAttribute : Attribute
{
    public GenerateMinimalApiAttribute()
    {
    }
    public GenerateMinimalApiAttribute(string apiName)
    {
        Name = apiName;
    }

    public string? Name { get; }
}
