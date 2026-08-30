using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

[GenerateSerializer]
public class InvokeResponseExceptionDto : InvokeResponseDoneDto
{
    public string ExceptionMessage { get; set; } = string.Empty;
}
