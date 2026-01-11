using gAPI.Helpers;
using gAPI.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace gAPI.AutoMapper;

public abstract class CustomMapping<TEntity, TDto>
    where TEntity : class
    where TDto : class
{
    public abstract Task<TDto> ToDtoAsync(
        TEntity source,
        TDto destination,
        ISecurityHandler<TDto>? serviceHandler = null);
    public abstract Task<TEntity> ToEntityAsync(
        TDto source,
        TEntity destination);

    public abstract IAsyncEnumerable<TDto> ProjectToDtosAsync(
        IQueryable<TEntity> source,
        string[]? orderby,
        int? skip,
        int? take,
        ISecurityHandler<TDto>? serviceHandler = null);

    protected virtual async IAsyncEnumerable<TDto> EnumerateDtosAsync(
        IEnumerable<TDto> items,
        ISecurityHandler<TDto>? serviceHandler = null)
    {
        foreach (var item in items)
        {
            if (item != null)
            {
                await ExtendDto(item, serviceHandler);
                yield return item;
            }
        }
    }

    protected virtual async Task ExtendDto(
        TDto item,
        ISecurityHandler<TDto>? serviceHandler = null)
    {
        if (item is ICrudEntity crudl)
        {
            crudl.CanUpdate = serviceHandler == null ? false : await serviceHandler.CanUpdateAsync(item);
            crudl.CanDelete = serviceHandler == null ? false : await serviceHandler.CanDeleteAsync(item);
        }
    }
}