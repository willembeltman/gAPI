using gAPI.Core.Attributes;
using gAPI.Core.Ids;

namespace gAPI.Core.Dtos;

[GenerateSerializer]
public record SynchronizeFabricIdsDto(
    FabricManagerId FabricManagerId,
    FabricConnectionId FabricConnectionId);
