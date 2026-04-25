namespace gAPI.Core.Server.Mappings;

public interface IStateUserMapping<TUser, TStateUserDto>
{
    Task<TStateUserDto> ToDtoAsync(TUser entity, TStateUserDto dto, CancellationToken ct);
}