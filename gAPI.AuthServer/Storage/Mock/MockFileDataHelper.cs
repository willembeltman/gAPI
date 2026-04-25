using System.Security.Cryptography;
using System.Text;

namespace gAPI.Storage.Mock;

public static class MockFileDataHelper
{
    public static async Task<MockFileData> ProcessStreamAsync(Stream stream, string fileName, string mimeType)
    {
        // Lees stream data
        byte[] fileData;
        if (stream.CanSeek)
        {
            stream.Position = 0;
            fileData = new byte[stream.Length];
            int totalRead = 0;
            while (totalRead < fileData.Length)
            {
                int read = await stream.ReadAsync(fileData, totalRead, fileData.Length - totalRead);
                if (read == 0) break; // einde stream
                totalRead += read;
            }
        }
        else
        {
            using (var memoryStream = new MemoryStream())
            {
                await stream.CopyToAsync(memoryStream);
                fileData = memoryStream.ToArray();
            }
        }

        // Bereken SHA256 hash
        string sha256Hash;
        using (var sha = SHA256.Create())
        {
            var hashBytes = sha.ComputeHash(fileData);
            sha256Hash = BytesToHex(hashBytes).ToLower();
        }

        // Maak MockFileData aan
        return new MockFileData
        {
            FileName = fileName,
            MimeType = mimeType,
            Data = fileData,
            Sha256 = sha256Hash,
            UploadedAt = DateTimeOffset.UtcNow
        };
    }

    private static string BytesToHex(byte[] bytes)
    {
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes)
            sb.Append(b.ToString("X2"));
        return sb.ToString();
    }
}
