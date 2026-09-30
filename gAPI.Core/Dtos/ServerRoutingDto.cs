using gAPI.Core.Attributes;
using gAPI.Core.Ids;

namespace gAPI.Core.Dtos;

[GenerateSerializer]
public record ServerRoutingDto(
    FabricManagerId From_FabricManagerId,
    FabricConnectionId From_FabricConnectionId,

    RequestId RequestId,
    ServiceId ServiceId,
    ServiceMethodId MethodId,

    FabricConnectionId FabricConnectionId,
    ClientConnectionId ClientConnectionId
    );