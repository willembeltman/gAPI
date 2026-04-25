namespace gAPI.Interfaces;

public interface IUseCase<TEntity, TDto, TKey>
{
    Task<bool> IsAllowedAsync(CancellationToken ct);
    Task<bool> CanCreateAsync(CancellationToken ct);
    Task<bool> CanListAsync(CancellationToken ct);
    Task<bool> CanCreateAsync(TDto dto, CancellationToken ct);
    Task<bool> CanReadAsync(TDto dto, CancellationToken ct);
    Task<bool> CanUpdateAsync(TDto dto, CancellationToken ct);
    Task<bool> CanDeleteAsync(TDto dto, CancellationToken ct);

    Task<TEntity?> FindByIdAsync(TKey id, CancellationToken ct);
    Task<TEntity?> FindByMatchAsync(TDto dto, CancellationToken ct);
    IQueryable<TEntity> ListAll();
    Task<bool> AddAsync(TEntity entity, CancellationToken ct);
    Task<bool> RemoveAsync(TEntity entity, CancellationToken ct);
    Task<bool> UpdateAsync(TEntity entity, TDto dto, CancellationToken ct);
}