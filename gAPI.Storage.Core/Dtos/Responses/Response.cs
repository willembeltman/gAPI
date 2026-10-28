using gAPI.Storage.Core.Enums;

namespace gAPI.Storage.Core.Dtos.Responses;


public class Response
{
    public bool Success { get; set; }
    public ErrorMessagesEnum? ErrorMessage { get; set; }
}