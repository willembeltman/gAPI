using gAPI.Enums;

namespace gAPI.Dtos;

public class BaseResponse
{
    public bool Success { get; set; }
    public BaseResponseErrorEnum? Error { get; set; }
    public string? RedirectPath { get; set; }
}
