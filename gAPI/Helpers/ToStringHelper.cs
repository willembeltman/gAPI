using System;
using System.Globalization;

#pragma warning disable IDE0060

namespace gAPI.Helpers
{
    public static class ToStringHelper
    {
        public static string ToString(this Guid guid, CultureInfo cultureInfo)
        {
            return guid.ToString();
        }
        public static string ToString(this Enum guid, CultureInfo cultureInfo)
        {
            return guid.ToString();
        }
    }
}
