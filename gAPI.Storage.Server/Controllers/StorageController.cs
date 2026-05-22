using gAPI.Core.Server.Storage.StorageServer.Dtos.Requests;
using gAPI.Core.Server.Storage.StorageServer.Dtos.Responses;
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
    public GetStorageFileInfoResponse GetStorageFileInfo(GetStorageFileInfoRequest model, CancellationToken ct)
    {
        return storageService.GetStorageFileUrl(model, ct);
    }

    [HttpPost]
    public SaveResponse SaveStorageFile([FromForm] SaveRequest model, IFormFile file, CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return new SaveResponse { Success = false, ErrorMessage = "No file uploaded" };

        var stream = file.OpenReadStream();
        return storageService.SaveStorageFile(model, stream, ct);
    }

    [HttpPost]
    public AppendResponse AppendStorageFile([FromForm] AppendRequest model, IFormFile file, CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return new AppendResponse { Success = false, ErrorMessage = "No file uploaded" };

        var stream = file.OpenReadStream();
        return storageService.AppendStorageFile(model, stream, ct);
    }

    [HttpPost]
    public DeleteResponse DeleteStorageFile(DeleteRequest model, CancellationToken ct)
    {
        return storageService.DeleteStorageFile(model, ct);
    }
}