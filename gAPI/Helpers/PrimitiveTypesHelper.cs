namespace gAPI.Helpers;

public static class PrimitiveTypesHelper
{
    public static string? GetSimpleCsTypeByFullName(string propertytype)
    {
        switch (propertytype)
        {
            case "System.Byte":
                return "byte";
            case "System.Int32":
                return "int";
            case "System.Int64":
                return "long";
            case "System.String":
                return "string";
            case "System.Double":
                return "double";
            case "System.Boolean":
                return "bool";
            case "System.Guid":
                return "Guid";
            case "System.DateTime":
                return "DateTime";
            default:
                return null;
        }
    }

    public static string GetSimpleCsTypeByName(string name)
    {
        switch (name)
        {
            case "Int64":
                return "long";
            case "Int32":
                return "int";
            case "String":
                return "string";
            case "Double":
                return "double";
            case "Boolean":
                return "bool";
            case "Guid":
                return "Guid";
            case "DateTime":
                return "DateTime";
            case "Byte":
                return "byte";
            default:
                return name;
        }
    }
}