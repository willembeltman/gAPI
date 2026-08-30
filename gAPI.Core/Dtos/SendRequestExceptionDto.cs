using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

[GenerateSerializer]
public class SendRequestExceptionDto : SendRequestDoneDto
{
    public string ExceptionMessage { get; set; } = string.Empty;
}
