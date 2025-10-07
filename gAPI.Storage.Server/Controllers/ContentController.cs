using gAPI.Storage.Server.Data;
using Microsoft.AspNetCore.Mvc;

namespace gAPI.Storage.Server.Controllers;

[ApiController]
[Route("[controller]")]
public class ContentController(ApplicationDbContext db) : ControllerBase
{
    static readonly DirectoryInfo Directory = new DirectoryInfo("Files");

    // GET /Content/{*path}?token=...
    [HttpGet("{*path}")]
    public IActionResult GetFile(string path, [FromQuery] string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return Unauthorized();
        if (string.IsNullOrWhiteSpace(path))
            return NotFound($"File '{path}' not found.");

        var split = path.Split('/');
        if (split.Length < 2)
            return NotFound($"File '{path}' not found.");

        var directoryName = split[0];
        var fileName = split[1];

        var folder = db.StorageFolders.FirstOrDefault(a => a.Name == directoryName);
        if (folder == null)
            return NotFound($"Folder for file '{path}' not found.");

        var file = db.StorageFiles.FirstOrDefault(a => a.FileName == fileName && a.StorageFolderId == folder.Id);
        if (file == null)
            return NotFound($"File '{path}' not found.");

        var filetoken = db.StorageFileTokens.FirstOrDefault(a => a.Token == token && a.StorageFileId == file.Id);
        if (filetoken == null)
            return Unauthorized($"Invalid token '{token}' for file '{path}'.");
        if (string.IsNullOrWhiteSpace(folder.Name))
            return NotFound($"Folder for file '{path}' not found.");
        if (string.IsNullOrWhiteSpace(file.FileName))
            return NotFound($"Filename for file '{path}' not found.");
        if (string.IsNullOrWhiteSpace(file.MimeType))
            return NotFound($"Mimetype for file '{path}' not found.");

        var directoryFullName = Path.Combine(Directory.FullName, folder.Name);
        var fullName = Path.Combine(directoryFullName, file.FileName);
        var fileInfo = new FileInfo(fullName);
        if (!fileInfo.Exists)
            return NotFound($"File '{path}' not found.");

        var fs = new FileStream(fullName, FileMode.Open, FileAccess.Read, FileShare.Read);
        return File(fs, file.MimeType, file.FileName);
    }
}