namespace gAPI.AutoComponent.Models.CrudModels;

public class HasLink
{
    public HasLink(string text, string controller, string? action)
    {
        this.Text = text;
        this.Controller = controller;
        this.Action = action;
    }

    public string Text { get; }
    public string Controller { get; }
    public string? Action { get; }
}