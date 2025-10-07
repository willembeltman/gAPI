using System;
using System.Linq;

namespace gAPI.AutoMapper
{
    public class MapperInstance<TEntity, TDto>(
        string code,
        Func<TEntity, TDto, TDto> toDtoDelegate,
        Func<TDto, TEntity, TEntity> toEntityDelegate,
        Func<IQueryable<TEntity>, IQueryable<TDto>> projectToDtosDelegate)
        where TDto : class
    {
        public string Code { get; } = code;

        public TDto ToDto(TEntity entity, TDto dto)
            => toDtoDelegate(entity, dto);

        public TEntity ToEntity(TDto dto, TEntity entity)
            => toEntityDelegate(dto, entity);

        public IQueryable<TDto> ProjectToDtos(IQueryable<TEntity> entities)
            => projectToDtosDelegate(entities);
    }
}
