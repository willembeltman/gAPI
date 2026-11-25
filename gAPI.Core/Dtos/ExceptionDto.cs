using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

[GenerateSerializer]
public class ExceptionDto
{
    public ExceptionDto()
    {
    }
    public ExceptionDto( 
        string message,
        string? innerMessage = null,
        string? stacktrace = null)
    {
        Message = message;
        InnerMessage = innerMessage;
        StackTrace = stacktrace ?? Environment.StackTrace;
    }
    public ExceptionDto(Exception ex)
    {
        Message = ex.Message;
        InnerMessage = ex.InnerException?.Message;
        StackTrace = ex.StackTrace;
    }
    public string Message { get; set; } = string.Empty;
    public string? InnerMessage { get; set; }
    public string? StackTrace { get; set; }
}