//using System;
//using System.Collections.Generic;
//using System.Text;
//using System.Text.RegularExpressions;

//namespace gAPI.Helpers
//{
//    internal static class RazorToRenderTreeMapping
//    {
//        /// <summary>
//        /// Zet Razor markup + code om naar een BuildRenderTree() methode + properties.
//        /// </summary>
//        public static (string renderTreeCode, string classProperties) Convert(string razorSource)
//        {
//            var builder = new StringBuilder();
//            var props = new StringBuilder();
//            var seq = 0;

//            // 1. Splits markup en @code blok
//            var codeBlockMatch = Regex.Match(razorSource, @"@code\s*\{([\s\S]*?)\}\s*$", RegexOptions.Multiline);
//            if (codeBlockMatch.Success)
//            {
//                props.AppendLine(codeBlockMatch.Groups[1].Value.Trim());
//                razorSource = razorSource.Substring(0, codeBlockMatch.Index).Trim();
//            }

//            // 2. Vereenvoudigde parser:
//            // - Zet HTML elementen om naar OpenElement/AddContent/CloseElement
//            // - Zet @if / @else naar normale C# if/else
//            // - Zet component tags (hoofdletter) om naar OpenComponent
//            // NB: Dit is geen volledige parser, maar werkt goed voor je componentenstructuur

//            var lines = razorSource.Split('\n');
//            foreach (var rawLine in lines)
//            {
//                var line = rawLine.Trim();

//                if (string.IsNullOrWhiteSpace(line))
//                    continue;

//                if (line.StartsWith("@if"))
//                {
//                    builder.AppendLine(line.Substring(1)); // verwijder '@'
//                    continue;
//                }
//                if (line.StartsWith("@else"))
//                {
//                    builder.AppendLine(line.Substring(1));
//                    continue;
//                }

//                // Component detectie (bijv. <ErrorView Response="Response" />)
//                var compMatch = Regex.Match(line, @"^<([A-Z]\w+)\s*(.*?)\s*/>$");
//                if (compMatch.Success)
//                {
//                    var compName = compMatch.Groups[1].Value;
//                    var attrs = compMatch.Groups[2].Value;
//                    builder.AppendLine($"__builder.OpenComponent<{compName}>({seq++});");
//                    foreach (var attr in ParseAttributes(attrs))
//                    {
//                        builder.AppendLine($"__builder.AddAttribute({seq++}, \"{attr.name}\", {attr.value});");
//                    }
//                    builder.AppendLine($"__builder.CloseComponent();");
//                    continue;
//                }

//                // HTML element (eenvoudig: opentag, content, sluit)
//                var elemMatch = Regex.Match(line, @"^<(\w+)(.*?)>(.*?)</\1>$");
//                if (elemMatch.Success)
//                {
//                    var tag = elemMatch.Groups[1].Value;
//                    var attrs = elemMatch.Groups[2].Value.Trim();
//                    var content = elemMatch.Groups[3].Value.Trim();

//                    builder.AppendLine($"__builder.OpenElement({seq++}, \"{tag}\");");
//                    foreach (var attr in ParseAttributes(attrs))
//                    {
//                        builder.AppendLine($"__builder.AddAttribute({seq++}, \"{attr.name}\", \"{attr.value}\");");
//                    }
//                    if (!string.IsNullOrEmpty(content))
//                    {
//                        builder.AppendLine($"__builder.AddContent({seq++}, \"{content}\");");
//                    }
//                    builder.AppendLine($"__builder.CloseElement();");
//                    continue;
//                }

//                // Losse tekst of @ChildContent etc.
//                if (line.StartsWith("@"))
//                {
//                    builder.AppendLine($"__builder.AddContent({seq++}, {line.Substring(1)});");
//                }
//                else
//                {
//                    builder.AppendLine($"__builder.AddContent({seq++}, \"{line}\");");
//                }
//            }

//            return (builder.ToString(), props.ToString());
//        }

//        private static List<(string name, string value)> ParseAttributes(string attrString)
//        {
//            var result = new List<(string name, string value)>();
//            if (string.IsNullOrWhiteSpace(attrString))
//                return result;

//            var matches = Regex.Matches(attrString, @"(\w+)=""([^""]*)""");
//            foreach (Match m in matches)
//            {
//                result.Add((m.Groups[1].Value, m.Groups[2].Value));
//            }
//            return result;
//        }
//    }
//}
