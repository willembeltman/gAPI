using System.Globalization;

#pragma warning disable IDE0060

namespace gAPI.Extensions;

public static class GuidToStringExtension
{
    public static string ToString(this Guid guid, CultureInfo cultureInfo)
    {
        return guid.ToString();
    }
}
