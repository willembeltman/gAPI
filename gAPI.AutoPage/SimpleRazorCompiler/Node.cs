using gAPI.AutoPage.SimpleRazorCompiler.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace gAPI.AutoPage.SimpleRazorCompiler;

public class Node(Node? parent, NodeTypeEnum nodeType, string? name = null)
{
    public Node? Parent { get; private set; } = parent;
    public NodeTypeEnum NodeType { get; private set; } = nodeType;
    public string? Name { get; private set; } = name; // XML heeft name

    public List<Node> Nodes { get; } = new List<Node>();
    public List<NodeAttribute> Attributes { get; } = new List<NodeAttribute>();
    public string? Text { get; private set; }
    public string? Code { get; private set; }

    protected int ReadBody(string markup, int position)
    {
        (var nodeType, var startIndex) = FindStart(markup, position);
        while (startIndex >= 0)
        {
            string between = markup
                .Substring(position, startIndex - position)
                .Replace("\r", "")
                .Replace("\n", "")
                .Trim();
            if (!string.IsNullOrEmpty(between))
            {
                // voeg text-node of ContentEnd toe
                Nodes.Add(new Node(this, NodeTypeEnum.Text) { Text = between });
            }

            switch (nodeType)
            {
                case NodeTypeEnum.Code:
                    position = ReadCode(markup, startIndex);
                    break;
                case NodeTypeEnum.Xml:
                    position = startIndex + 1;

                    if (markup[position] == '/')
                    {
                        position = position + 1;
                        (var endPosition, var endType) = FindEnd(markup, position);

                        if (endType == EndTypeEnum.Closed)
                            position = endPosition + 2;
                        else
                            position = endPosition + 1;
                        return position;
                    }
                    else
                    {
                        var startDebug = position;
                        var spaceIndex = markup.IndexOf(" ", position);
                        (int endIndex, EndTypeEnum endType) = FindEnd(markup, position);
                        var name = string.Empty;
                        if (endIndex < spaceIndex)
                        {
                            if (endIndex >= 0)
                            {
                                // Geen space gevonden
                                name = markup.Substring(position, endIndex - position);
                                if (endType == EndTypeEnum.Closed)
                                {
                                    // Node compleet afsluiten
                                    position = endIndex + 2;
                                    break;
                                }
                                position = endIndex + 1;

                                var closedXmlNode = new Node(this, NodeTypeEnum.Xml, name);
                                position = closedXmlNode.ReadBody(markup, position);
                                Nodes.Add(closedXmlNode);
                                break;
                            }
                            else throw new Exception("");
                        }
                        else if (spaceIndex == -1)
                        {
                            spaceIndex = endIndex;
                        }

                        name = markup.Substring(position, spaceIndex - position);
                        position = spaceIndex + 1;

                        if (name == "!--")
                        {
                            // Geen space gevonden
                            (endIndex, endType) = FindEnd(markup, position);
                            if (endIndex > 0)
                            {
                                position = endIndex + 1;
                                break;
                            }
                            else throw new Exception();
                        }

                        var xmlNode = new Node(this, NodeTypeEnum.Xml, name);
                        position = xmlNode.ReadXmlAttributesThenBody(markup, position);
                        Nodes.Add(xmlNode);
                    }
                    break;
                default:
                    break;
            }

            (nodeType, startIndex) = FindStart(markup, position);
        }

        // alles na de laatste node bewaren als ContentEnd
        if (position < markup.Length)
        {
            string tail = markup
                .Substring(position)
                .Replace("\r", "")
                .Replace("\n", "")
                .Trim();
            if (!string.IsNullOrEmpty(tail))
            {
                Nodes.Add(new Node(this, NodeTypeEnum.Text) { Text = tail });
            }
        }

        return markup.Length;
    }

    private int ReadXmlAttributesThenBody(string markup, int position)
    {
        position = ReadXmlAttributes(markup, position);

        (int endIndex, EndTypeEnum endType) = FindEnd(markup, position);
        if (endIndex > 0)
        {
            if (endType == EndTypeEnum.Closed)
            {
                // Node compleet afsluiten
                position = endIndex + 2;
                return position;
            }
            position = endIndex + 1;
            position = ReadBody(markup, position);
        }
        return position;
    }
    private int ReadXmlAttributes(string markup, int position)
    {
        while (position < markup.Length)
        {
            // Skip whitespace
            while (position < markup.Length && char.IsWhiteSpace(markup[position]))
                position++;

            // Check voor einde van de tag
            if (position >= markup.Length || markup[position] == '>' ||
                (markup[position] == '/' && position + 1 < markup.Length && markup[position + 1] == '>'))
                break;

            // Lees attribuutnaam
            int startName = position;
            while (position < markup.Length && !char.IsWhiteSpace(markup[position]) &&
                   markup[position] != '=' && markup[position] != '>' && markup[position] != '/')
            {
                position++;
            }

            string name = markup.Substring(startName, position - startName);

            // Skip whitespace
            while (position < markup.Length && char.IsWhiteSpace(markup[position]))
                position++;

            string? value = null;
            if (position < markup.Length && markup[position] == '=')
            {
                position++; // skip '='

                // Skip whitespace
                while (position < markup.Length && char.IsWhiteSpace(markup[position]))
                    position++;

                if (position < markup.Length && (markup[position] == '"' || markup[position] == '\''))
                {
                    char quote = markup[position];
                    position++;
                    int startValue = position;

                    while (position < markup.Length)
                    {
                        if (markup[position] == '@' && position + 1 < markup.Length && markup[position + 1] == '(')
                        {
                            // Razor expressie: skip tot bijbehorende ')'
                            position += 2; // skip '@('
                            int depth = 1;
                            while (position < markup.Length && depth > 0)
                            {
                                if (markup[position] == '(') depth++;
                                else if (markup[position] == ')') depth--;
                                position++;
                            }
                            continue;
                        }

                        if (markup[position] == quote)
                            break;

                        position++;
                    }

                    value = markup.Substring(startValue, position - startValue);
                    if (position < markup.Length) position++; // sluitende quote
                }
                else
                {
                    // Unquoted waarde
                    int startValue = position;
                    while (position < markup.Length && !char.IsWhiteSpace(markup[position]) && markup[position] != '>')
                        position++;
                    value = markup.Substring(startValue, position - startValue);
                }
            }

            Attributes.Add(new NodeAttribute(name, value));
        }

        return position;
    }

    protected int ReadCode(string markup, int position)
    {
        if (position >= markup.Length || markup[position] != '@')
            throw new ArgumentException("Position must point at '@'");

        position++; // skip '@'

        // skip whitespace after '@'
        while (position < markup.Length && char.IsWhiteSpace(markup[position]))
            position++;

        // case: @{ ... }  (explicit code block)
        if (position < markup.Length && markup[position] == '{')
        {
            string block = ReadBlockInnnerAcolade(markup, ref position);
            var codeChild = new Node(this, NodeTypeEnum.SubCode) { Code = "{" + block + "}" };
            Nodes.Add(codeChild);
            return position;
        }

        // case: @( ... ) inline expression
        if (position < markup.Length && markup[position] == '(')
        {
            string block = ReadBlockInnerHaakje(markup, ref position);
            var codeChild = new Node(this, NodeTypeEnum.Text) { Text = "@" + block };
            Nodes.Add(codeChild);
            return position;
        }

        // lees header tot eerste whitespace, '{', '(', '<', of einde
        string headerCode = ReadHeaderCode(markup, ref position);

        // case: <td>@...</td> expression
        if (markup[position] == '<')
        {
            var textNode = new Node(this, NodeTypeEnum.Text) { Text = "@" + headerCode };
            Nodes.Add(textNode);
            return position;
        }

        var start = position;
        // check of dit een control statement is
        while (headerCode == "if" || headerCode == "else" || headerCode == "foreach" || headerCode == "while" || headerCode == "for")
        {
            // neem alles mee tot aan de eerste '{'
            int startExpr = position;
            int depth = 0;
            bool inDQ = false, inSQ = false;

            while (position < markup.Length)
            {
                char c = markup[position];
                if (c == '"' && !inSQ && !IsEscaped(markup, position)) inDQ = !inDQ;
                else if (c == '\'' && !inDQ && !IsEscaped(markup, position)) inSQ = !inSQ;
                else if (!inDQ && !inSQ)
                {
                    if (c == '(') depth++;
                    else if (c == ')') depth--;
                    else if (c == '{' && depth == 0)
                        break; // hier stopt de header
                }
                position++;
            }

            var condition = markup.Substring(startExpr, position - startExpr).Trim();
            var strs = new string[] { headerCode, condition }
                .Where(a => !string.IsNullOrWhiteSpace(a));
            var code = string.Join(" ", strs);
            var headerNode = new Node(this, NodeTypeEnum.SubCode) { Code = code };
            Nodes.Add(headerNode);

            // nu komt de body { ... }
            if (position < markup.Length && markup[position] == '{')
            {
                string inner = ReadBlockInnnerAcolade(markup, ref position);
                var innerStructure = new CodeNode(inner);
                foreach (var n in innerStructure.Nodes)
                {
                    n.Parent = headerNode;
                    headerNode.Nodes.Add(n);
                }
            }

            // verder lezen, sla witruimte en enters over
            while (position < markup.Length &&
                   (markup[position] == ' ' ||
                   markup[position] == '\r' ||
                   markup[position] == '\n' ||
                   markup[position] == '\t'))
            {
                position++;
            }

            // voor volgende pass, lees header tot eerste whitespace, '{', '(', '<', of einde
            start = position;
            headerCode = ReadHeaderCode(markup, ref position);
        }

        position = start;

        return position;
    }

    private static string ReadHeaderCode(string markup, ref int position)
    {
        int headerStartPos = position;
        while (position < markup.Length &&
               !char.IsWhiteSpace(markup[position]) &&
               markup[position] != '{' &&
               markup[position] != '(' &&
               markup[position] != '<')
        {
            position++;
        }

        string headerCode = markup
            .Substring(headerStartPos, position - headerStartPos);
        return headerCode;
    }

    private static string ReadBlockInnerHaakje(string s, ref int pos)
    {
        if (pos >= s.Length || s[pos] != '(')
            throw new ArgumentException("Pos must point at '('");

        int i = pos + 1;
        int depth = 1;
        bool inDQ = false, inSQ = false;

        while (i < s.Length && depth > 0)
        {
            char c = s[i];
            if (c == '"' && !inSQ && !IsEscaped(s, i)) inDQ = !inDQ;
            else if (c == '\'' && !inDQ && !IsEscaped(s, i)) inSQ = !inSQ;
            else if (!inDQ && !inSQ)
            {
                if (c == '(') depth++;
                else if (c == ')') depth--;
            }

            if (depth > 0) i++; // alleen incrementeren als je nog IN het block zit
        }

        if (depth != 0)
            throw new Exception("Unbalanced haakjes in code block");

        // nu staat i op de afsluitende ')' 
        var inner = s.Substring(pos + 1, i - pos - 1);
        pos = i + 1; // nu direct na de afsluitende '}'
        return inner;
    }
    private static string ReadBlockInnnerAcolade(string s, ref int pos)
    {
        if (pos >= s.Length || s[pos] != '{')
            throw new ArgumentException("Pos must point at '{'");

        int i = pos + 1;
        int depth = 1;
        bool inDQ = false, inSQ = false;

        while (i < s.Length && depth > 0)
        {
            char c = s[i];
            if (c == '"' && !inSQ && !IsEscaped(s, i)) inDQ = !inDQ;
            else if (c == '\'' && !inDQ && !IsEscaped(s, i)) inSQ = !inSQ;
            else if (!inDQ && !inSQ)
            {
                if (c == '{') depth++;
                else if (c == '}') depth--;
            }

            if (depth > 0) i++; // alleen incrementeren als je nog IN het block zit
        }

        if (depth != 0)
            throw new Exception("Unbalanced braces in code block");

        // nu staat i op de afsluitende '}' 
        var inner = s.Substring(pos + 1, i - pos - 1);
        pos = i + 1; // nu direct na de afsluitende '}'
        return inner;
    }

    private static bool IsEscaped(string s, int idx)
    {
        int backslashes = 0;
        int j = idx - 1;
        while (j >= 0 && s[j] == '\\')
        {
            backslashes++;
            j--;
        }
        return backslashes % 2 == 1;
    }

    private static (NodeTypeEnum, int) FindStart(string markup, int position)
    {
        var startIndexAt = markup.IndexOf("@", position);
        var startIndexOpen = markup.IndexOf("<", position);
        if (startIndexAt >= 0 && startIndexOpen >= 0)
        {
            if (startIndexAt < startIndexOpen)
            {
                return (NodeTypeEnum.Code, startIndexAt);
            }
            else
            {
                return (NodeTypeEnum.Xml, startIndexOpen);
            }
        }
        if (startIndexAt > 0)
        {
            return (NodeTypeEnum.Code, startIndexAt);
        }
        else
        {
            return (NodeTypeEnum.Xml, startIndexOpen);
        }
    }
    private static (int, EndTypeEnum) FindEnd(string markup, int position)
    {
        var endEndIndex = markup.IndexOf("/>", position);
        var endNormalIndex = markup.IndexOf(">", position);
        if (endEndIndex > 0)
        {
            if (endNormalIndex < endEndIndex)
            {
                return (endNormalIndex, EndTypeEnum.Normal);
            }
            else
            {
                return (endEndIndex, EndTypeEnum.Closed);
            }
        }
        return (endNormalIndex, EndTypeEnum.Normal);
    }
}