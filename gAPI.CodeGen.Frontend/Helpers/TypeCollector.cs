using gAPI.CodeGen.Frontend.Models.Configs;
using System.Reflection;

namespace gAPI.CodeGen.Frontend.Helpers;

public class TypeCollector
{
    private readonly HashSet<Type> Seen = new HashSet<Type>();
    private readonly HashSet<Type> Added = new HashSet<Type>();

    public TypeCollector(FrontendConfig config)
    {
        Config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public List<Type> Dtos { get; } = new List<Type>();
    public List<Type> Enums { get; } = new List<Type>();
    public FrontendConfig Config { get; }

    public void Add(Type type)
    {
        if (type == null || !Seen.Add(type))
            return;

        if (type.IsEnum)
        {
            if (IsValidToAdd(type))
                Enums.Add(type);
        }
        else if (type.IsClass || type.IsValueType) // struct is ook ValueType, maar niet enum
        {
            var definition = type.IsGenericType ? type.GetGenericTypeDefinition() : type;

            if (IsValidToAdd(definition))
                Dtos.Add(definition);

            // Publieke properties ophalen
            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                Add(prop.PropertyType);
            }

            // Generieke argumenten recursief toevoegen
            if (type.IsGenericType)
            {
                foreach (var arg in type.GetGenericArguments())
                    Add(arg);
            }
        }
        else if (type.IsArray)
        {
            Add(type!.GetElementType()!);
        }
        else if (type.IsGenericType)
        {
            foreach (var arg in type.GetGenericArguments())
                Add(arg);
        }
    }

    private bool IsValidToAdd(Type type)
    {
        if (type == null)
            return false;

        if (!Added.Add(type))
            return false;

        var ns = type.Namespace ?? "";
        return Config.BaseNamespaces.Any(a => ns.StartsWith(a));
    }
}
