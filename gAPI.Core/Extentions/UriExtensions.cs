using gAPI.Core.Extensions;
using System.Text;

namespace gAPI.Core.Extentions;

public static class UriExtensions
{
    public static string UriToBase64String(this Uri uri)
    {
        return uri.ToString().ToBase64String();
    }
    public static Uri UriFromBase64String(this string uriBase64String)
    {
        return new Uri(uriBase64String.StringFromBase64String());
    }
}
