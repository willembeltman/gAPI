using gAPI.AutoComponents.SimpleRazorCompiler.Enums;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace gAPI.AutoComponents.SimpleRazorCompiler;

public static class BuildRenderTree
{
    public static string Compile(this CodeNode structure)
    {
        var builderIndex = 0;
        var sb = new StringBuilder();
        sb.AppendLine($"        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder __builder{builderIndex})");
        sb.AppendLine("        {");
        sb.AppendLine("            var seq = 0;");
        foreach (var node in structure.Nodes)
        {
            ProcessNode(node, sb, "            ", "seq", builderIndex);
        }
        sb.Append("        }");
        return sb.ToString();
    }

    private static void ProcessNode(Node node, StringBuilder sb, string indent, string seqVar, int builderIndex)
    {
        switch (node.NodeType)
        {
            case NodeTypeEnum.Xml:
                HandleXml(node, sb, indent, seqVar, builderIndex);
                break;

            case NodeTypeEnum.Text:
                HandleText(node, sb, indent, seqVar, builderIndex);
                break;

            case NodeTypeEnum.Code:
            case NodeTypeEnum.SubCode:
                HandleCode(node, sb, indent, seqVar, builderIndex);
                break;
        }
    }

    private static void HandleXml(Node node, StringBuilder sb, string indent, string seqVar, int builderIndex)
    {
        if (node.Name == "InputFile" || node.Name == "InputText" || node.Name == "InputSelect")
        {
            if (node.Name == "InputSelect")
            {
                var t = node.Attributes.FirstOrDefault(a => a.Name == "bindtype_Value")
                    ?? throw new System.Exception("InputSelect needs a bindtype attribute");

                // Blazor component (conventie: hoofdletter start)
                sb.AppendLine($"{indent}__builder{builderIndex}.OpenComponent<InputSelect<{t.Value}>>({seqVar}++);");
            }
            else
            {
                // Normaal HTML element
                sb.AppendLine($"{indent}__builder{builderIndex}.OpenComponent<{node.Name}>({seqVar}++);");
            }
        }
        else
        {
            // Normaal HTML element
            sb.AppendLine($"{indent}__builder{builderIndex}.OpenElement({seqVar}++, \"{node.Name}\");");
        }

        // Attributes
        if (node.Attributes != null)
        {
            foreach (var attr in node.Attributes)
            {
                string name = attr.Name;
                string value = attr.Value ?? "";

                if (name.StartsWith("bindtype_"))
                {
                    continue;
                }

                if (name == "OnChange")
                {
                    sb.AppendLine($"{indent}__builder{builderIndex}.AddAttribute({seqVar}++, \"OnChange\", EventCallback.Factory.Create<InputFileChangeEventArgs>(this, {value}));");
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

        // Children
        if (node.Name == "InputSelect")
        {
            if (node.Nodes.Count > 0)
            {
                var newBuilderIndex = builderIndex + 1;
                sb.AppendLine($"{indent}__builder{builderIndex}.AddAttribute(seq++, \"ChildContent\", (RenderFragment)((__builder{newBuilderIndex}) =>");
                sb.AppendLine($"{indent}{{");
                foreach (var child in node.Nodes)
                {
                    ProcessNode(child, sb, indent + "    ", seqVar, newBuilderIndex);
                }
                sb.AppendLine($"{indent}}}));");
            }
        }
        else
        {
            if (node.Nodes.Count > 0)
            {
                sb.AppendLine($"{indent}{{");
                foreach (var child in node.Nodes)
                {
                    ProcessNode(child, sb, indent + "    ", seqVar, builderIndex);
                }
                sb.AppendLine($"{indent}}}");
            }
        }

        if (node.Name == "InputFile" || node.Name == "InputText" || node.Name == "InputSelect")
        {
            sb.AppendLine($"{indent}__builder{builderIndex}.CloseComponent();");
        }
        else
        {
            sb.AppendLine($"{indent}__builder{builderIndex}.CloseElement();");
        }
    }

    private static void HandleText(Node node, StringBuilder sb, string indent, string seqVar, int builderIndex)
    {
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
                ProcessNode(child, sb, indent + "    ", seqVar, builderIndex);
            }
            sb.AppendLine($"{indent}}}");
        }
    }
    private static void HandleCode(Node node, StringBuilder sb, string indent, string seqVar, int builderIndex)
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
                ProcessNode(child, sb, indent + "    ", seqVar, builderIndex);
            }
            sb.AppendLine($"{indent}}}");
        }
    }

}
