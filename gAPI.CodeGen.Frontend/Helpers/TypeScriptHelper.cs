namespace gAPI.CodeGen.Frontend.Helpers;

public static class TypeScriptHelper
{
    public static string? GetTsType(string propertytype)
    {
        switch (propertytype)
        {
            case "System.Int64":
            case "System.Int32":
            case "System.Double":
            case "System.Byte":
                return "number";
            case "System.String":
                return "string";
            case "System.DateTime":
                return "Date";
            case "System.Boolean":
                return "boolean";
            case "System.Guid":
                return "string";
            case "Microsoft.AspNetCore.Http.IFormFile":
                return "File";
            default:
                return null;
        }
    }
}
