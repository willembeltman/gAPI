using gAPI.Core.Attributes;
using gAPI.Core.Ids;

namespace gAPI.Core.Dtos;

[GenerateSerializer]
public record GetSessionCookieDataResponseDto(
    SessionId SessionId, 
    string? CookieData);
