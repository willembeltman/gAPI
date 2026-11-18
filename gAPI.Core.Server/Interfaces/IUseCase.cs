namespace gAPI.Core.Server.Interfaces;

public interface IUseCase<TDbContext, TEntity, TDto, TKey>
{
    Task<bool> IsAllowedAsync(TDbContext dbContext, CancellationToken ct);
    Task<bool> CanCreateAsync(TDbContext dbContext, CancellationToken ct);
    Task<bool> CanListAsync(TDbContext dbContext, CancellationToken ct);
    Task<bool> CanCreateAsync(TDbContext dbContext, TDto dto, CancellationToken ct);
    Task<bool> CanReadAsync(TDbContext dbContext, TDto dto, CancellationToken ct);
    Task<bool> CanUpdateAsync(TDbContext dbContext, TDto dto, CancellationToken ct);
    Task<bool> CanDeleteAsync(TDbContext dbContext, TDto dto, CancellationToken ct);

    Task<TEntity?> FindByIdAsync(TDbContext dbContext, TKey id, CancellationToken ct);
    Task<TEntity?> FindByMatchAsync(TDbContext dbContext, TDto dto, CancellationToken ct);
    IQueryable<TEntity> ListAll(TDbContext dbContext);
    Task<bool> AddAsync(TDbContext dbContext, TEntity entity, CancellationToken ct);
    Task<bool> RemoveAsync(TDbContext dbContext, TEntity entity, CancellationToken ct);
    Task<bool> UpdateAsync(TDbContext dbContext, TEntity entity, TDto dto, CancellationToken ct);
}