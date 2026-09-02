using gAPI.Core.Attributes;
using gAPI.Core.Ids;

namespace gAPI.Core.Dtos;

[GenerateSerializer]
public record SynchronizeClientIdsDto(
    FabricManagerId FabricManagerId,
    FabricConnectionId FabricConnectionId,
    ClientConnectionId ClientConnectionId);

