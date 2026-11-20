using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

[GenerateSerializer]
public record InitializeDto(
    string? StateData,
    string SessionId);