using gAPI.Core.Server.Storage.StorageServer.Enums;

namespace gAPI.Core.Server.Storage.StorageServer.Dtos.Responses;


public class Response
{
    public bool Success { get; set; }
    public ErrorMessagesEnum? ErrorMessage { get; set; }
}