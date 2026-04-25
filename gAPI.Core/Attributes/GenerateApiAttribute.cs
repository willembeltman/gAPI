namespace gAPI.Attributes;

[AttributeUsage(AttributeTargets.Interface)]
public class GenerateApiAttribute : Attribute
{
    public GenerateApiAttribute()
    {
    }
    public GenerateApiAttribute(string apiName)
    {
        Name = apiName;
    }

    public string? Name { get; }
}
