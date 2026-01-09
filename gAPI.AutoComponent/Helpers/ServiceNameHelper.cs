namespace gAPI.AutoComponent.Helpers;

public static class ServiceNameHelper
{
    static string ServiceNameEnd = "Service";
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
}
