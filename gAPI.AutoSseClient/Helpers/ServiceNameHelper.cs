namespace gAPI.AutoSseClient.Helpers;

internal static class ServiceNameHelper
{
    static string ServiceNameEnd = "Service";
    static string ClientHandlerNameEnd = "ClientHandler";
    public static string RemoveInterfacePrefix(string apiName)
    {
        if (apiName.StartsWith("I"))
            apiName = apiName.Substring(1);
        return apiName;
    }
    public static string RemoveServiceName(string name)
    {
        if (name.ToLower().EndsWith(ServiceNameEnd.ToLower()))
            return name.Substring(0, name.Length - ServiceNameEnd.Length);
        return name;
    }
    public static string RemoveClientHandlerName(string name)
    {
        if (name.ToLower().EndsWith(ClientHandlerNameEnd.ToLower()))
            return name.Substring(0, name.Length - ClientHandlerNameEnd.Length);
        return name;
    }
}
