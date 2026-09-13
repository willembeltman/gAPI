using gAPI.Core.Ids;

namespace gAPI.Core.Dtos;

public record RequestArgumentIndexDto(
    RequestId RequestId, 
    int ArgumentIndex);