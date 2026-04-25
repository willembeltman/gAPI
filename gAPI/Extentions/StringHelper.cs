using System.Security.Cryptography;
using System.Text;

namespace gAPI.Extensions;

public static class StringHelper
{
    public static string HashString(string input)
    {
        using var sha256Hash = SHA256.Create();
        var data = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

        // Convert the byte array to a hexadecimal string.
        var builder = new StringBuilder();
        for (int i = 0; i < data.Length; i++)
        {
            builder.Append(data[i].ToString("x2"));
        }
        return builder.ToString();
    }

    public static string ToBase64String(this string value)
    {
        var buffer = Encoding.UTF8.GetBytes(value);
        var base64State = Convert.ToBase64String(buffer);
        return base64State;
    }
    public static string StringFromBase64String(this string value)
    {
        var buffer = Convert.FromBase64String(value);
        var result = Encoding.UTF8.GetString(buffer);
        return result;
    }
}