using gAPI.Core.Attributes;
using gAPI.Storage.Core.Dtos.Requests;
using gAPI.Storage.Core.Dtos.Responses;

namespace gAPI.Storage.Core;

[GenerateBus]
public interface IStorageServiceBus
{
    Task<AppendResponse> AppendStorageFile(AppendRequest model, IAsyncEnumerable<ReadOnlyMemory<byte>> inputStream, CancellationToken ct);
    Task<DeleteResponse> DeleteStorageFile(DeleteRequest model, CancellationToken ct);
    Task<GetStorageFileInfoResponse> GetStorageFileUrl(GetStorageFileInfoRequest model, CancellationToken ct);
    Task<SaveResponse> SaveStorageFile(SaveRequest model, IAsyncEnumerable<ReadOnlyMemory<byte>> inputStream, CancellationToken ct);
}