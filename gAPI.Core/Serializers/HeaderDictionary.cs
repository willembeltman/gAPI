using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace gAPI.Core.Serializers;

public class HeaderDictionary : Dictionary<string, StringValues>, IHeaderDictionary
{
    public HeaderDictionary() : base(StringComparer.OrdinalIgnoreCase)
    {
    }

    public long? ContentLength { get; set; }
}