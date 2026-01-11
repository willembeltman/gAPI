namespace gAPI.Dtos;

public class BaseResponse
{
    public bool Success { get; set; }
    public bool ErrorGettingState { get; set; }
    public bool ErrorItemNotSupplied { get; set; }
    public bool ErrorNotAuthorized { get; set; }
    public bool ErrorItemNotFound { get; set; }
    public bool ErrorAlreadyUsed { get; set; }
    public bool ErrorAttachingState { get; set; }
    public bool ErrorUpdatingState { get; set; }
    public bool ErrorGettingData { get; set; }
    public string? RedirectPath { get; set; }
}
