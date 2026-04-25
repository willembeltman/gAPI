using System.Runtime.CompilerServices;

namespace gAPI.Interfaces;

public abstract class Mapping<TEntity, TDto>
{
    public abstract Task<TDto> ToDtoAsync(TEntity entity, TDto dto, CancellationToken ct);
    public abstract TEntity ToEntity(TDto dto, TEntity entity);
    public abstract IAsyncEnumerable<TDto> ProjectToDtosAsync(IQueryable<TEntity> entities, string[]? orderby, int? skip, int? take, CancellationToken ct);
    public abstract Task ExtendDto(TDto dto, CancellationToken ct);

    public async IAsyncEnumerable<TDto> EnumerateDtosAsync(IEnumerable<TDto> items, [EnumeratorCancellation] CancellationToken ct)
    {
        foreach (var item in items)
        {
            if (item != null)
            {
                await ExtendDto(item, ct);
                yield return item;
            }
        }
    }
}