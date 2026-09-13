using gAPI.Core.Ids;

namespace gAPI.Core.Dtos;

public record RequestArgumentIndexStreamDto(
    RequestId RequestId,
    int ArgumentIndex,
    StreamId StreamId);
