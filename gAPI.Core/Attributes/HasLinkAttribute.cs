namespace gAPI.Core.Attributes;

public class HasLinkAttribute : Attribute
{
    public HasLinkAttribute(string text, string controller, string? page = null)
    {
        Text = text;
        Controller = controller;
        Action = page;
    }

    public string Text { get; }
    public string Controller { get; }
    public string? Action { get; }
}
