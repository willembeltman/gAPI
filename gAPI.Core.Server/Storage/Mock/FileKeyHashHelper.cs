using System;
using System.Security.Cryptography;
using System.Text;

namespace gAPI.Storage.Mock;

public static class FileKeyHashHelper
{
    public static string GetFileKeyHash(string fileKey)
    {
        var input = $"{fileKey}_{DateTimeOffset.UtcNow:yyyyMMddHH}";
        var bytes = Encoding.UTF8.GetBytes(input);

        using (var sha = SHA256.Create())
        {
            var hashBytes = sha.ComputeHash(bytes);
            return BytesToHex(hashBytes).ToLower().Substring(0, 16);
        }
    }

    private static string BytesToHex(byte[] bytes)
    {
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes)
            sb.Append(b.ToString("X2"));
        return sb.ToString();
    }
}
