using System;

namespace gAPI.AutoApi.Helpers
{
    internal static class StringCaseHelper
    {
        internal static string ToNameCase(this string value)
        {
            return value.Substring(0, 1).ToUpper() + value.Substring(1, value.Length - 1);
        }
        internal static string ToCamelCase(this string value)
        {
            return value.Substring(0, 1).ToLower() + value.Substring(1, value.Length - 1);
        }
        internal static string TrimEnd(this string value, string trim)
        {
            if (!value.EndsWith(trim, StringComparison.CurrentCultureIgnoreCase)) return value;
            return value.Substring(0, value.Length - trim.Length);
        }
        internal static string ToMultiple(this string value)
        {
            if (value.EndsWith("y"))
            {
                return value.Substring(0, value.Length - 1) + "ies";
            }
            return value + "s";
        }
    }
}