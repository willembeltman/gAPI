using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

[GenerateSerializer]
public record ServerInvokeRequestDto(
    ServerRoutingDto Routing,
    string Path,
    string? QueryString,
    byte[] IpAddress,
    string? CookieData,
    string? StateData,
    string? SessionData,
    byte[] BinaryData);