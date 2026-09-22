using gAPI.Core.Attributes;
using gAPI.Core.Dtos;
using gAPI.Storage.LanCloud.Api.Models;
using gAPI.Storage.LanCloud.Shared.Dtos;

namespace gAPI.Storage.LanCloud.Shared.Interfaces;

[GenerateApi]
public interface IFileSystemApi
{
    Task<AuthStateUserDto?> AuthenticateUser(string? userName, string? password, CancellationToken ct);
    Task CreateDirectory(string path, CancellationToken ct);
    Task Delete(string path, CancellationToken ct);
    Task<FileSystemEntry?> Get(string path, CancellationToken ct);
    Task<AuthenticationInfo> GetAuthenticationInfo(CancellationToken ct);
    IAsyncEnumerable<FileSystemEntry> ListDirectory(string path, CancellationToken ct);
    Task Move(string sourcePath, string destinationPath, CancellationToken ct);

    IAsyncEnumerable<byte[]> OpenReadByteArray(string path, long startOffset, CancellationToken ct);
    IAsyncEnumerable<ReadOnlyMemory<byte>> OpenReadReadOnlyMemoryByte(string path, long startOffset, CancellationToken ct);
    Task WriteAsyncEnumerableByte(string path, long startOffset, IAsyncEnumerable<byte[]> stream, CancellationToken ct);
    Task AppendAsyncEnumerableByte(string path, IAsyncEnumerable<byte[]> stream, CancellationToken ct);
    Task WriteByteArray(string path, long startOffset, byte[] buffer, CancellationToken ct);
    Task AppendByteArray(string path, byte[] buffer, CancellationToken ct);
    Task WriteReadOnlyMemory(string path, long startOffset, ReadOnlyMemory<byte> buffer, CancellationToken ct);
    Task AppendReadOnlyMemory(string path, ReadOnlyMemory<byte> buffer, CancellationToken ct);
}