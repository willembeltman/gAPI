using gAPI.Core.Attributes;
using gAPI.Core.Server.Storage.StorageServer.Dtos.Requests;
using gAPI.Core.Server.Storage.StorageServer.Dtos.Responses;

namespace gAPI.Storage.Server.Services;

[GenerateBus]
public interface IGapiStorageService
{
    Task<AppendResponse> AppendStorageFile(AppendRequest model, IAsyncEnumerable<ReadOnlyMemory<byte>> inputStream, CancellationToken ct);
    Task<DeleteResponse> DeleteStorageFile(DeleteRequest model, CancellationToken ct);
    Task<GetStorageFileInfoResponse> GetStorageFileUrl(GetStorageFileInfoRequest model, CancellationToken ct);
    Task<SaveResponse> SaveStorageFile(SaveRequest model, IAsyncEnumerable<ReadOnlyMemory<byte>> inputStream, CancellationToken ct);
}