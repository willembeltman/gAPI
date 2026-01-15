namespace gAPI.Interfaces;

public abstract class Mapping<TEntity, TDto>
{
    public abstract Task<TDto> ToDtoAsync(TEntity entity, TDto dto);
    public abstract Task<TEntity> ToEntityAsync(TDto dto, TEntity entity);
    public abstract IAsyncEnumerable<TDto> ProjectToDtosAsync(IQueryable<TEntity> entities, string[]? orderby, int? skip, int? take);
    public abstract Task ExtendDto(TDto dto);

    public async IAsyncEnumerable<TDto> EnumerateDtosAsync(IEnumerable<TDto> items)
    {
        foreach (var item in items)
        {
            if (item != null)
            {
                await ExtendDto(item);
                yield return item;
            }
        }
    }
}