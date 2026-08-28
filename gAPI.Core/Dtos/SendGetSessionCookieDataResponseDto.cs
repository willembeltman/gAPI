using gAPI.Core.Attributes;
using gAPI.Core.Ids;

namespace gAPI.Core.Dtos;

[GenerateSerializer]
public record SendGetSessionCookieDataResponseDto(
    SessionId SessionId, 
    string? CookieData);
