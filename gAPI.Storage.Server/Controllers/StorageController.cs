using gAPI.Core.Server.Storage.StorageServer.Dtos.Requests;
using gAPI.Core.Server.Storage.StorageServer.Dtos.Responses;
using gAPI.Core.Server.Storage.StorageServer.Enums;
using gAPI.Storage.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace gAPI.Storage.Server.Controllers;


[Authorize]
[ApiController]
[Route("[controller]/[action]")]
public class StorageController(LocalStorageService storageService) : ControllerBase
{
    [HttpPost]
    public Task<GetStorageFileInfoResponse> GetStorageFileInfo(GetStorageFileInfoRequest model, CancellationToken ct)
    {
        return storageService.GetStorageFileUrl(model, ct);
    }

    [HttpPost]
    public async Task<SaveResponse> SaveStorageFile([FromForm] SaveRequest model, IFormFile file, CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return new SaveResponse { Success = false, ErrorMessage = ErrorMessagesEnum.NoFileUploaded };

        var stream = file.OpenReadStream();
        return await storageService.SaveStorageFile(model, stream, ct);
    }

    [HttpPost]
    public async Task<AppendResponse> AppendStorageFile([FromForm] AppendRequest model, IFormFile file, CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return new AppendResponse { Success = false, ErrorMessage = ErrorMessagesEnum.NoFileUploaded };

        var stream = file.OpenReadStream();
        return await storageService.AppendStorageFile(model, stream, ct);
    }

    [HttpPost]
    public Task<DeleteResponse> DeleteStorageFile(DeleteRequest model, CancellationToken ct)
    {
        return storageService.DeleteStorageFile(model, ct);
    }
}