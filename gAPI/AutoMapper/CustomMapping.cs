using gAPI.Helpers;
using gAPI.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace gAPI.AutoMapper
{
    /// <summary>
    /// Extensible base for mapping between <typeparamref name="TEntity"/> and <typeparamref name="TDto"/>.
    /// <para>
    /// By default, this base delegates all mapping to <see cref="MapperInstance{TEntity, TDto}"/> 
    /// and is therefore directly usable without overrides.
    /// </para>
    /// <para>
    /// Subclasses may:
    /// <list type="bullet">
    /// <item><description>completely replace the mapping logic,</description></item>
    /// <item><description>decorate or extend the default mapper’s output,</description></item>
    /// <item><description>inject custom projection logic (e.g. navigation properties),</description></item>
    /// <item><description>enrich DTOs after mapping via <see cref="ExtendDto"/>.</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <typeparam name="TEntity">The source (entity) type.</typeparam>
    /// <typeparam name="TDto">The destination (DTO) type.</typeparam>
    public abstract class CustomMapping<TEntity, TDto>
        where TEntity : class
        where TDto : class
    {
        /// <summary>
        /// Maps an entity to a DTO asynchronously.
        /// Override this method to add or override fields
        /// after the <paramref name="defaultMapper"/> has performed its work.
        /// </summary>
        /// <param name="source">The entity instance to map from.</param>
        /// <param name="destination">The DTO instance to populate.</param>
        /// <param name="defaultMapper">The default mapper that performs baseline property mappings.</param>
        /// <param name="serviceHandler">Optional service or security handler that can enrich the DTO.</param>
        /// <returns>The mapped DTO.</returns>
        public virtual async Task<TDto> ToDtoAsync(
            TEntity source,
            TDto destination,
            MapperInstance<TEntity, TDto> defaultMapper,
            ISecurityHandler<TDto>? serviceHandler)
        {
            var dto = defaultMapper.ToDto(source, destination);
            await ExtendDto(dto, serviceHandler);
            return dto;
        }

        /// <summary>
        /// Maps a DTO back into an entity asynchronously.
        /// Override this to add or override custom mapping rules
        /// after the <paramref name="defaultMapper"/> has copied basic values.
        /// </summary>
        /// <param name="source">The DTO to map from.</param>
        /// <param name="destination">The entity instance to populate.</param>
        /// <param name="defaultMapper">The default mapper that performs baseline property mappings.</param>
        /// <returns>The mapped entity.</returns>
        public virtual Task<TEntity> ToEntityAsync(
            TDto source,
            TEntity destination,
            MapperInstance<TEntity, TDto> defaultMapper)
        {
            return Task.FromResult(defaultMapper.ToEntity(source, destination));
        }

        /// <summary>
        /// Projects a queryable set of entities into DTOs, applying ordering, paging, and optional enrichment.
        /// Override this to customize query projection behavior (e.g. include navigation properties).
        /// </summary>
        /// <param name="source">The queryable entity set.</param>
        /// <param name="orderby">Optional ordering instructions.</param>
        /// <param name="skip">Number of items to skip (for paging).</param>
        /// <param name="take">Maximum number of items to return (for paging).</param>
        /// <param name="defaultMapper">The default mapper to use for projection.</param>
        /// <param name="serviceHandler">Optional service handler used to enrich each DTO.</param>
        /// <returns>An async enumerable of DTOs.</returns>
        public virtual IAsyncEnumerable<TDto> ProjectToDtosAsync(
            IQueryable<TEntity> source,
            string[]? orderby,
            int? skip,
            int? take,
            MapperInstance<TEntity, TDto> defaultMapper,
            ISecurityHandler<TDto>? serviceHandler)
        {
            var queryable = defaultMapper
                .ProjectToDtos(source)
                .ApplyOrderBy(orderby);
            if (skip != null)
                queryable = queryable.Skip(skip.Value);
            if (take != null)
                queryable = queryable.Take(take.Value);
            return EnumerateDtosAsync(queryable, serviceHandler);
        }

        /// <summary>
        /// Iterates over projected DTOs and allows enrichment via <see cref="ExtendDto"/>.
        /// Typically you do not override this method, but you can if you want to change
        /// the iteration or enrichment pipeline.
        /// </summary>
        /// <param name="items">The DTO collection to iterate over.</param>
        /// <param name="serviceHandler">Optional service handler used to enrich each DTO.</param>
        /// <returns>An async enumerable of enriched DTOs.</returns>
        protected virtual async IAsyncEnumerable<TDto> EnumerateDtosAsync(
            IEnumerable<TDto> items,
            ISecurityHandler<TDto>? serviceHandler)
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

        /// <summary>
        /// Extension hook to modify or enrich a single DTO after mapping.
        /// Override this to add fields such as permissions, computed properties,
        /// or values from external services.
        /// </summary>
        /// <param name="item">The DTO being processed.</param>
        /// <param name="serviceHandler">Optional service handler that can provide security or enrichment info.</param>
        protected virtual async Task ExtendDto(
            TDto item,
            ISecurityHandler<TDto>? serviceHandler)
        {
            if (item is ICrudEntity crudl)
            {
                crudl.CanUpdate = serviceHandler == null ? false : await serviceHandler.CanUpdateAsync(item);
                crudl.CanRemove = serviceHandler == null ? false : await serviceHandler.CanRemoveAsync(item);
            }
        }
    }
}