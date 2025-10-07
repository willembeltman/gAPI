using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace gAPI.Helpers
{
    public static class StringHelper
    {
        public static string HashString(string input)
        {
            using (var sha256Hash = SHA256.Create())
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(input)))
            {
                var data = sha256Hash.ComputeHash(stream);

                // Convert the byte array to a hexadecimal string.
                var builder = new StringBuilder();
                for (int i = 0; i < data.Length; i++)
                {
                    builder.Append(data[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}