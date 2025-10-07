using gAPI.Storage.StorageServer.Dtos.Requests;
using gAPI.Storage.StorageServer.Dtos.Responses;
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
    public GetStorageFileInfoResponse GetStorageFileInfo(GetStorageFileInfoRequest model)
    {
        return storageService.GetStorageFileUrl(model);
    }

    [HttpPost]
    public SaveResponse SaveStorageFile([FromForm] SaveRequest model, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return new SaveResponse { Success = false, Message = "No file uploaded" };

        var stream = file.OpenReadStream();
        return storageService.SaveStorageFile(model, stream);
    }

    [HttpPost]
    public DeleteResponse DeleteStorageFile(DeleteRequest model)
    {
        return storageService.DeleteStorageFile(model);
    }
}