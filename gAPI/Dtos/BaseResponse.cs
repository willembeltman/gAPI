namespace gAPI.Dtos
{
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
        public void SetResponse(BaseResponse response)
        {
            Success = response.Success;
            ErrorGettingState = response.ErrorGettingState;
            ErrorItemNotSupplied = response.ErrorItemNotSupplied;
            ErrorNotAuthorized = response.ErrorNotAuthorized;
            ErrorItemNotFound = response.ErrorItemNotFound;
            ErrorAlreadyUsed = response.ErrorAlreadyUsed;
            ErrorAttachingState = response.ErrorAttachingState;
            ErrorUpdatingState = response.ErrorUpdatingState;
            ErrorGettingData = response.ErrorGettingData;
            RedirectPath = response.RedirectPath;
        }
    }
}
