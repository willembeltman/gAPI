namespace gAPI.AutoMapper;

public abstract class CustomMapping<TEntity, TDto>
    where TEntity : class
    where TDto : class
{
    public abstract Task<TDto> ToDtoAsync(
        TEntity source,
        TDto destination,
        MapperInstance<TEntity, TDto> defaultMapper);
    public abstract Task<TEntity> ToEntityAsync(
        TDto source,
        TEntity destination,
        MapperInstance<TEntity, TDto> defaultMapper);

    public abstract IAsyncEnumerable<TDto> ProjectToDtosAsync(
        IQueryable<TEntity> source,
        string[]? orderby,
        int? skip,
        int? take,
        MapperInstance<TEntity, TDto> defaultMapper);

    protected virtual async IAsyncEnumerable<TDto> EnumerateDtosAsync(
        IEnumerable<TDto> items)
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

    protected virtual async Task ExtendDto(
        TDto item)
    {
    }
}