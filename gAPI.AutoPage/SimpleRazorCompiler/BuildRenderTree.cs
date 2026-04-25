using gAPI.AutoPage.Interfaces;
using gAPI.SimpleRazorCompiler.Enums;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace gAPI.SimpleRazorCompiler;

public static class BuildRenderTree
{
    public static string Compile(this CodeNode structure, ISharedReference[] AllComponents, List<string> usings)
    {
        var builderIndex = 0;
        var sb = new StringBuilder();
        sb.AppendLine($"        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder __builder{builderIndex})");
        sb.AppendLine("        {");
        sb.AppendLine("            var seq0 = 0;");
        foreach (var node in structure.Nodes)
        {
            ProcessNode(node, sb, "            ", builderIndex, AllComponents, usings);
        }
        sb.Append("        }");
        return sb.ToString();
    }

    private static void ProcessNode(Node node, StringBuilder sb, string indent, int builderIndex, ISharedReference[] specialControls, List<string> usings)
    {
        switch (node.NodeType)
        {
            case NodeTypeEnum.Xml:
                HandleXml(node, sb, indent, builderIndex, specialControls, usings);
                break;

            case NodeTypeEnum.Text:
                HandleText(node, sb, indent, builderIndex, specialControls, usings);
                break;

            case NodeTypeEnum.Code:
            case NodeTypeEnum.SubCode:
                HandleCode(node, sb, indent, builderIndex, specialControls, usings);
                break;
        }
    }

    private static void HandleXml(Node node, StringBuilder sb, string indent, int builderIndex, ISharedReference[] assemblyControls, List<string> usings)
    {
        string seqVar = $"seq{builderIndex}";

        bool hasNormalChildContext = false;
        string? specialClildContext = null;
        string? specialNode = null;
        switch (node.Name)
        {
            case "InputSelect":
            case "NavLink":
            case "InputNumber":
                hasNormalChildContext = true;
                break;
            case "EditForm":
                specialClildContext = "EditContext";
                break;
            case "Authorized":
            case "NotAuthorized":
                specialNode = "global::Microsoft.AspNetCore.Components.Authorization.AuthenticationState";
                break;
        }

        // Speciale nodes
        if (specialNode != null)
        {
            var contextAttr = node.Attributes.FirstOrDefault(a => a.Name == "Context")?.Value ?? "context";
            var newBuilderIndex = builderIndex + 1;
            var newSeqVar = $"seq{newBuilderIndex}";
            sb.AppendLine($@"
{indent}__builder{builderIndex}.AddAttribute(
{indent}    {seqVar}++, 
{indent}    ""{node.Name}"", 
{indent}    (RenderFragment<{specialNode}>)(({(contextAttr)}) => 
{indent}    (RenderFragment)(__builder{newBuilderIndex} => 
{indent}{{
{indent}    var {newSeqVar} = 0;");
            foreach (var child in node.Nodes)
            {
                ProcessNode(child, sb, indent + "    ", newBuilderIndex, assemblyControls, usings);
            }
            sb.AppendLine($"{indent}}})));");
            return;
        }

        // Component of Element?
        var componentControl = assemblyControls.FirstOrDefault(a => a.Name == node.Name);
        if (componentControl != null)
        {
            usings.Add($"using {componentControl.Namespace};");
            var bindType = node.Attributes.FirstOrDefault(a => a.Name == "bindtype_Value");
            if (bindType != null && hasNormalChildContext)
            {
                // Component met bind type
                sb.AppendLine($"{indent}__builder{builderIndex}.OpenComponent<{node.Name}<{bindType.Value}>>({seqVar}++);");
            }
            else
            {
                // Gewoon component
                sb.AppendLine($"{indent}__builder{builderIndex}.OpenComponent<{node.Name}>({seqVar}++);");
            }
        }
        else
        {
            // Element
            sb.AppendLine($"{indent}__builder{builderIndex}.OpenElement({seqVar}++, \"{node.Name}\");");
        }

        // Attributes toevoegen        
        if (node.Attributes != null)
        {
            AddAttributes(node, sb, indent, builderIndex, seqVar);
        }

        // Children toevoegen
        if (node.Nodes.Count > 0)
        {
            if (specialClildContext != null)
            {
                var newBuilderIndex = builderIndex + 1;
                var newSeqVar = $"seq{newBuilderIndex}";
                sb.AppendLine($@"
{indent}__builder{builderIndex}.AddAttribute(seq{builderIndex}++, ""ChildContent"", (RenderFragment<{specialClildContext}>)((_) => (RenderFragment)((__builder{newBuilderIndex}) =>
{indent}{{
{indent}    var {newSeqVar} = 0;");
                foreach (var child in node.Nodes)
                {
                    ProcessNode(child, sb, indent + "    ", newBuilderIndex, assemblyControls, usings);
                }
                sb.AppendLine($"{indent}}})));");
            }
            else if (hasNormalChildContext)
            {
                var newBuilderIndex = builderIndex + 1;
                var newSeqVar = $"seq{newBuilderIndex}";
                sb.AppendLine($@"
{indent}__builder{builderIndex}.AddAttribute({seqVar}++, ""ChildContent"", (RenderFragment)((__builder{newBuilderIndex}) =>
{indent}{{
{indent}    var {newSeqVar} = 0;");
                foreach (var child in node.Nodes)
                {
                    ProcessNode(child, sb, indent + "    ", newBuilderIndex, assemblyControls, usings);
                }
                sb.AppendLine($"{indent}}}));");
            }
            else
            {
                sb.AppendLine($"{indent}{{");
                foreach (var child in node.Nodes)
                {
                    ProcessNode(child, sb, indent + "    ", builderIndex, assemblyControls, usings);
                }
                sb.AppendLine($"{indent}}}");
            }
        }

        // Node afsluiten
        if (componentControl != null) // Component
        {
            sb.AppendLine($"{indent}__builder{builderIndex}.CloseComponent();");
        }
        else // Normaal element
        {
            sb.AppendLine($"{indent}__builder{builderIndex}.CloseElement();");
        }
    }

    private static void AddAttributes(Node node, StringBuilder sb, string indent, int builderIndex, string seqVar)
    {
        foreach (var attr in node.Attributes.Where(a =>
                        !a.Name.StartsWith("bindtype_")))
        {
            string name = attr.Name;
            string value = attr.Value ?? "";

            if ((node.Name == "EditForm" && name == "Model") ||
                (node.Name == "ErrorView" && name == "Response"))
            {
                sb.AppendLine($"{indent}__builder{builderIndex}.AddAttribute({seqVar}++, \"{name}\", {value});");
                continue;
            }
            if (node.Name == "EditForm" && name == "OnValidSubmit")
            {
                sb.AppendLine($"{indent}__builder{builderIndex}.AddAttribute({seqVar}++, \"{name}\", EventCallback.Factory.Create<EditContext>(this, {value}));");
                continue;
            }
            if (name == "OnChange")
            {
                sb.AppendLine($"{indent}__builder{builderIndex}.AddAttribute({seqVar}++, \"{name}\", EventCallback.Factory.Create<InputFileChangeEventArgs>(this, {value}));");
                continue;
            }
            if (name == "@ref")
            {
                name = name.Substring(1);
                sb.AppendLine($"{indent}__builder{builderIndex}.AddElementReferenceCapture({seqVar}++, __value => {{ {value} = __value; }});");
                continue;
            }
            if (name.StartsWith("@bind-"))
            {
                var subname = name.Substring("@bind-".Length);
                var bindtypeName = "bindtype_" + subname;
                var bindtypeAttribute = node.Attributes.First(a => a.Name == bindtypeName);

                sb.AppendLine($"{indent}__builder{builderIndex}.AddAttribute({seqVar}++, \"{subname}\", {value});");
                sb.AppendLine($"{indent}__builder{builderIndex}.AddAttribute({seqVar}++, \"{subname}Changed\", EventCallback.Factory.Create<{bindtypeAttribute.Value}>(this, __value => {value} = __value));");
                sb.AppendLine($"{indent}__builder{builderIndex}.AddAttribute({seqVar}++, \"{subname}Expression\", (System.Linq.Expressions.Expression<Func<{bindtypeAttribute.Value}>>)(() => {value}));");
                continue;
            }
            if (name.StartsWith("@"))
            {
                name = name.Substring(1);
                sb.AppendLine($"{indent}__builder{builderIndex}.AddAttribute({seqVar}++, \"{name}\", EventCallback.Factory.Create(this, {value}));");
                continue;
            }

            // default
            if (value.StartsWith("@"))
            {
                value = value.Substring(1);
            }
            else
            {
                if (value.Contains("@"))
                {
                    // vervang @(...) door { ... } in een interpolated string
                    value = Regex.Replace(value, @"@\(.*?\)", match =>
                    {
                        string inner = match.Value.Substring(2, match.Value.Length - 3); // haal @(...) weg
                        return $"{{({inner})}}";
                    });

                    value = $"$\"{value}\"";
                }
                else
                {
                    value = $"\"{value}\"";
                }
            }
            sb.AppendLine($"{indent}__builder{builderIndex}.AddAttribute({seqVar}++, \"{name}\", {value});");
        }
    }

    private static void HandleText(Node node, StringBuilder sb, string indent, int builderIndex, ISharedReference[] specialControls, List<string> usings)
    {
        var seqVar = $"seq{builderIndex}";
        string text = node.Text!;
        if (!string.IsNullOrEmpty(text))
        {
            if (text.StartsWith("@"))
            {
                text = text.Substring(1);
            }
            else
            {
                if (text.Contains("@"))
                {
                    // vervang @(...) door { ... } in een interpolated string
                    text = Regex.Replace(text, @"@\(.*?\)", match =>
                    {
                        string inner = match.Value.Substring(2, match.Value.Length - 3); // haal @(...) weg
                        return $"{{({inner})}}";
                    });

                    text = $"$\"{text}\"";
                }
                else
                {
                    text = $"\"{text}\"";
                }
            }
            sb.AppendLine($"{indent}__builder{builderIndex}.AddContent({seqVar}++, {text});");
        }

        // Children
        if (node.Nodes.Count > 0)
        {
            sb.AppendLine($"{indent}{{");
            foreach (var child in node.Nodes)
            {
                ProcessNode(child, sb, indent + "    ", builderIndex, specialControls, usings);
            }
            sb.AppendLine($"{indent}}}");
        }
    }
    private static void HandleCode(Node node, StringBuilder sb, string indent, int builderIndex, ISharedReference[] specialControls, List<string> usings)
    {
        string code = node.Code!;
        if (!string.IsNullOrEmpty(code))
        {
            // Escape quotes
            sb.AppendLine($"{indent}{code}");
        }

        // Children
        if (node.Nodes.Count > 0)
        {
            sb.AppendLine($"{indent}{{");
            foreach (var child in node.Nodes)
            {
                ProcessNode(child, sb, indent + "    ", builderIndex, specialControls, usings);
            }
            sb.AppendLine($"{indent}}}");
        }
    }

}
