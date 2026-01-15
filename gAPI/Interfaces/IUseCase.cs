namespace gAPI.Interfaces;

public interface IUseCase<TEntity, TDto, TKey>
{
    Task<bool> IsAllowedAsync();
    Task<bool> CanCreateAsync();
    Task<bool> CanCreateAsync(TDto dto);
    Task<bool> CanDeleteAsync(TDto dto);
    Task<bool> CanListAsync();
    Task<bool> CanReadAsync(TDto dto);
    Task<bool> CanUpdateAsync(TDto dto);

    Task<TEntity?> FindByIdAsync(TKey id);
    Task<TEntity?> FindByMatchAsync(TDto dto);
    IQueryable<TEntity> ListAll();
    Task<bool> AddAsync(TEntity entity);
    Task<bool> RemoveAsync(TEntity entity);
    Task<bool> UpdateAsync(TEntity entity, TDto dto);

    //Task<TDto> ToDtoAsync(TEntity entity, TDto dto);
    //Task<TEntity> ToEntityAsync(TDto dto, TEntity entity);
    //IAsyncEnumerable<TDto> ProjectToDtosAsync(IQueryable<TEntity> entities, string[]? orderby, int? skip, int? take);
}