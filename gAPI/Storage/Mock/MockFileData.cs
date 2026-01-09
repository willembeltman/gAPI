using System;

namespace gAPI.Storage.Mock;

// Mock file data storage
public class MockFileData
{
    public string FileName { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public byte[] Data { get; set; } = Array.Empty<byte>();
    public string Sha256 { get; set; } = string.Empty;
    public DateTimeOffset UploadedAt { get; set; }
}