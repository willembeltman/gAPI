using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

[GenerateSerializer]
public record ServerSendRequestDto(
    ServerRoutingDto Routing,
    string Path,
    string? QueryString,
    byte[] IpAddress,
    string? CookieData,
    string? StateData,
    string? SessionData,
    byte[] BinaryData);