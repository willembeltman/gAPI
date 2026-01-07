using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace gAPI.AutoSse.Configs
{
    internal static class ServerConfigParser
    {
        internal static ServerConfig Parse(string json)
        {
            return new ServerConfig
            {
                BaseNamespaces = ParseStringList(json, nameof(ServerConfig.BaseNamespaces)),
                SseContexts_Destination = ParseBlock(json, nameof(ServerConfig.SseContexts_Destination)),
                SseServices_Destination = ParseBlock(json, nameof(ServerConfig.SseServices_Destination)),
                AddAutoSseServices_Destination = ParseBlock(json, nameof(ServerConfig.AddAutoSseServices_Destination)),
                SseHostController_Destination = ParseBlock(json, nameof(ServerConfig.SseHostController_Destination))
            };
        }

        private static NamespaceConfig ParseBlock(string json, string blockName)
        {
            // Match de hele block (tussen {} na de key)
            var blockMatch = Regex.Match(json, $"\"{blockName}\"\\s*:\\s*\\{{(.*?)\\}}", RegexOptions.Singleline);
            if (!blockMatch.Success)
                return new NamespaceConfig();

            string blockContent = blockMatch.Groups[1].Value;

            string dir = TryExtract(blockContent, "\"Directory\"\\s*:\\s*\"([^\"]+)\"");
            string ns = TryExtract(blockContent, "\"Namespace\"\\s*:\\s*\"([^\"]+)\"");

            return new NamespaceConfig
            {
                Directory = dir ?? "",
                Namespace = ns ?? ""
            };
        }
        private static string[] ParseStringList(string json, string propertyName)
        {
            var match = Regex.Match(json, $"\"{propertyName}\"\\s*:\\s*\\[(.*?)\\]", RegexOptions.Singleline);
            if (!match.Success)
                return Array.Empty<string>();

            string arrayContent = match.Groups[1].Value;

            // Match individuele strings binnen de array
            var matches = Regex.Matches(arrayContent, "\"([^\"]+)\"");
            return matches.Cast<Match>().Select(m => m.Groups[1].Value).ToArray();
        }


        private static string TryExtract(string input, string pattern)
        {
            var match = Regex.Match(input, pattern);
            return match.Success ? match.Groups[1].Value : null;
        }
    }
}