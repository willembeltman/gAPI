namespace gAPI.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class IsComponentAttribute(string title, string submitText = "Submit", string responseText = "Response") : Attribute
{
    public string Tile { get; } = title;
    public string SubmitText { get; } = submitText;
    public string ResponseText { get; } = responseText;
}
