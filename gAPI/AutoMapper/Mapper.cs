using gAPI.AutoMapper.Engine;
using gAPI.Helpers;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace gAPI.AutoMapper
{
    public static class Mapper
    {
        private static readonly ConcurrentDictionary<MapperKey, object> MapperCache =
            new ConcurrentDictionary<MapperKey, object>();

        public static IServiceProvider? ServiceProvider { get; private set; }

        public static void SetServiceProvider(IServiceProvider provider)
            => ServiceProvider = provider;

        public static async Task<TDto> ToDtoAsync<TEntity, TDto>(this TEntity? entity, TDto? dto, ISecurityHandler<TDto>? handler = null)
            where TEntity : class
            where TDto : class
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            var defaultMapper = GetInstance<TEntity, TDto>();

            IServiceScope? serviceScope = null;
            CustomMapping<TEntity, TDto>? customMapper = null;
            if (ServiceProvider != null)
            {
                serviceScope = ServiceProvider.CreateScope();
                customMapper = serviceScope?.ServiceProvider
                    .GetService<CustomMapping<TEntity, TDto>>();
            }

            if (customMapper == null)
            {
                dto = defaultMapper.ToDto(entity, dto);
            }
            else
            {
                var customResult = await customMapper.ToDtoAsync(entity, dto, defaultMapper, handler);
                if (customResult == null)
                {
                    // Assume nothing has been done to the destination
                    dto = defaultMapper.ToDto(entity, dto);
                }
                else
                {
                    dto = customResult;
                }
            }

            serviceScope?.Dispose();

            return dto;
        }

        public static async Task<TEntity> ToEntityAsync<TDto, TEntity>(this TDto? dto, TEntity? entity)
            where TDto : class
            where TEntity : class
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            var defaultMapper = GetInstance<TEntity, TDto>();

            IServiceScope? serviceScope = null;
            CustomMapping<TEntity, TDto>? customMapper = null;
            if (ServiceProvider != null)
            {
                serviceScope = ServiceProvider.CreateScope();
                customMapper = serviceScope?.ServiceProvider
                    .GetService<CustomMapping<TEntity, TDto>>();
            }

            if (customMapper == null)
            {
                entity = defaultMapper.ToEntity(dto, entity);
            }
            else
            {
                var customResult = await customMapper.ToEntityAsync(dto, entity, defaultMapper);
                if (customResult == null)
                {
                    // Assume nothing has been done to the destination
                    entity = defaultMapper.ToEntity(dto, entity);
                }
                else
                {
                    entity = customResult;
                }
            }

            serviceScope?.Dispose();

            return entity;
        }

        public static async IAsyncEnumerable<TDto> ProjectToDtosAsync<TEntity, TDto>(
            this IQueryable<TEntity>? entities,
            string[]? orderby,
            int? skip,
            int? take,
            ISecurityHandler<TDto>? serviceHandler = null)
            where TEntity : class
            where TDto : class
        {
            if (entities == null)
                throw new ArgumentNullException(nameof(entities));

            IServiceScope? serviceScope = null;
            try
            {
                var defaultMapper = GetInstance<TEntity, TDto>();

                CustomMapping<TEntity, TDto>? customMapper = null;
                if (ServiceProvider != null)
                {
                    serviceScope = ServiceProvider.CreateScope();
                    customMapper = serviceScope?.ServiceProvider
                        .GetService<CustomMapping<TEntity, TDto>>();
                }

                if (customMapper == null)
                {
                    var defaultProjection = defaultMapper
                        .ProjectToDtos(entities)
                        .ApplyOrderBy(orderby);
                    if (skip != null)
                        defaultProjection = defaultProjection.Skip(skip.Value);
                    if (take != null)
                        defaultProjection = defaultProjection.Take(take.Value);
                    foreach (var item in defaultProjection)
                        yield return item;
                }
                else
                {
                    var customProjection = customMapper
                        .ProjectToDtosAsync(entities, orderby, skip, take, defaultMapper, serviceHandler);
                    if (customProjection == null)
                    {
                        // Assume nothing has been done to the destination
                        var defaultProjection = defaultMapper
                            .ProjectToDtos(entities)
                            .ApplyOrderBy(orderby);
                        if (skip != null)
                            defaultProjection = defaultProjection.Skip(skip.Value);
                        if (take != null)
                            defaultProjection = defaultProjection.Take(take.Value);
                        foreach (var item in defaultProjection)
                            yield return item;
                    }
                    else
                    {
                        await foreach (var item in customProjection)
                            yield return item;
                    }
                }
            }
            finally
            {
                serviceScope?.Dispose();
            }
        }

        private static MapperInstance<TIn, TOut> GetInstance<TIn, TOut>()
            where TIn : class
            where TOut : class
        {
            var key = new MapperKey(typeof(TIn), typeof(TOut));
            return (MapperInstance<TIn, TOut>)MapperCache.GetOrAdd(
                key, _ => MapperFactory<TIn, TOut>.CreateInstance());
        }
    }
}
