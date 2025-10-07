using gAPI.AutoMapper.Models;
using gAPI.Helpers;
using Microsoft.CodeAnalysis;
using System;
using System.Linq;

namespace gAPI.AutoMapper.Engine
{
    internal static class MapperFactory<TEntity, TDto>
        where TDto : class
    {
        public static MapperInstance<TEntity, TDto> CreateInstance()
        {
            var typeIn = typeof(TEntity);
            var typeOut = typeof(TDto);
            var className = $"{typeIn.FullName.Replace(".", "")}{typeOut.FullName.Replace(".", "")}Mapper";
            var @namespace = "gAPI.AutoMapper.GeneratedMappers";
            var toDtoMethodName = "ToDto";
            var toEntityMethodName = "ToEntity";
            var projectToDtosMethodName = "ProjectToDtos";
            var fullClassName = $"{@namespace}.{className}";

            var code = GenerateCode(typeIn, typeOut, className, @namespace, toDtoMethodName, toEntityMethodName, projectToDtosMethodName);

            var asm = CodeCompiler.Compile(code);
            var serializerType = asm.GetType(fullClassName);
            var toDtoMethod = serializerType.GetMethod(toDtoMethodName);
            var toEntityMethod = serializerType.GetMethod(toEntityMethodName);
            var projectToDtosMethod = serializerType.GetMethod(projectToDtosMethodName);

            var toDtoDelegate = (Func<TEntity, TDto, TDto>)Delegate.CreateDelegate(
                typeof(Func<TEntity, TDto, TDto>), toDtoMethod);

            var toEntityDelegate = (Func<TDto, TEntity, TEntity>)Delegate.CreateDelegate(
                typeof(Func<TDto, TEntity, TEntity>), toEntityMethod);

            var projectToDtosDelegate = (Func<IQueryable<TEntity>, IQueryable<TDto>>)Delegate.CreateDelegate(
                typeof(Func<IQueryable<TEntity>, IQueryable<TDto>>), projectToDtosMethod);

            return new MapperInstance<TEntity, TDto>(code, toDtoDelegate, toEntityDelegate, projectToDtosDelegate);
        }

        private static string GenerateCode(Type typeEntity, Type typeDto, string className, string @namespace, string toDtoMethodName, string toEntityMethodName, string projectToDtosMethodName)
        {
            var context = new EntityToDtoModel(typeEntity, typeDto);

            return $@"using System;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using gAPI.Helpers;

#nullable disable
namespace {@namespace}
{{
    public static class {className}
    {{
        public static {typeDto.FullName} {toDtoMethodName}({typeEntity.FullName} entity, {typeDto.FullName} dto)
        {{
		    if (entity == null || dto == null) return null;
{string.Join("",
    context.MatchedDtoProperties
        .Select(a => $@"
            dto.{a.Name} = entity.{a.Name};"))}
{string.Join("",
    context.DtoNameProperties
        .Select(a => $@"
            dto.{a.Name} = 
                {string.Join(" + \" \" + \r\n                ",
            a.ForeignEntityNameProperties!
                .Select(b => b.IsName?.Format($"entity?.{a.EntityForeignNavigationProperty!.Name}?.{b.Name}"))
        )};"))}
            return dto;
        }}

        public static {typeEntity.FullName} {toEntityMethodName}({typeDto.FullName} dto, {typeEntity.FullName} entity)
        {{
		    if (dto == null || entity == null) return null;
{string.Join("",
    context.MatchedDtoProperties
        .Select(a => $@"
            entity.{a.Name} = dto.{a.Name};"))}
            return entity;
        }}

        public static IQueryable<{typeDto.FullName}> {projectToDtosMethodName}(IQueryable<{typeEntity.FullName}> query)
        {{  
            return query
                .Select(entity => new {typeDto.FullName}()
                {{{string.Join("",
    context.MatchedDtoProperties
        .Select(a => $@"
                    {a.Name} = entity.{a.Name},"))}{string.Join("",
    context.DtoNameProperties
        .Select(a => $@"
                    {a.Name} = 
                        {string.Join(" + \" \" + \r\n                        ",
        a.ForeignEntityNameProperties
            .Select(b => b.IsName.Format($"entity.{a.EntityForeignNavigationProperty!.Name}.{b.Name}"))
    )},"))}
                }});
        }}
    }}
}}";

        }
    }
}
