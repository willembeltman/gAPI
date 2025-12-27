using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

if (args.Length != 2)
{
    Console.Error.WriteLine("Usage: dotnet gapi-config <template.json> <appsettings.json>");
    return;
}

var templatePath = args[0];
var targetPath = args[1];

if (!File.Exists(templatePath) || !File.Exists(targetPath))
{
    Console.Error.WriteLine("Template or target file not found.");
    return;
}

var template = JsonNode.Parse(await File.ReadAllTextAsync(templatePath))!;
var target = JsonNode.Parse(await File.ReadAllTextAsync(targetPath))!;

bool changed = Merge(template, target);

if (changed)
{
    var opts = new JsonSerializerOptions { WriteIndented = true };
    await File.WriteAllTextAsync(targetPath, target.ToJsonString(opts));
    Console.WriteLine("✔ gAPI config injected");
}
else
{
    Console.WriteLine("ℹ gAPI config already present");
}

static bool Merge(JsonNode source, JsonNode target)
{
    bool changed = false;

    if (source is JsonObject src && target is JsonObject dst)
    {
        foreach (var kv in src)
        {
            if (!dst.ContainsKey(kv.Key))
            {
                dst[kv.Key] = Process(kv.Value);
                changed = true;
            }
            else
            {
                changed |= Merge(kv.Value!, dst[kv.Key]!);
            }
        }
    }

    return changed;
}

static JsonNode Process(JsonNode? node)
{
    if (node is JsonValue v &&
        v.TryGetValue<string>(out var s) &&
        s == "__GENERATE__")
    {
        return RandomHex(32);
    }

    if (node is JsonObject o)
    {
        var clone = new JsonObject();
        foreach (var kv in o)
            clone[kv.Key] = Process(kv.Value);
        return clone;
    }

    return node!.DeepClone();
}

static string RandomHex(int len)
{
    var bytes = RandomNumberGenerator.GetBytes(len / 2);
    return Convert.ToHexString(bytes);
}
