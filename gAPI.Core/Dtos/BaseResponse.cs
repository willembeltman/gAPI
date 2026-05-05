using gAPI.Core.Enums;

namespace gAPI.Core.Dtos;

public class BaseResponse
{
    public bool Success { get; set; }
    public BaseResponseErrorEnum? Error { get; set; }
    public string? RedirectPath { get; set; }
}
