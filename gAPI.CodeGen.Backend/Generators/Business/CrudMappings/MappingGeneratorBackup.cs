//using gAPI.CodeGen.Backend.Data.Entities;
//using gAPI.CodeGen.Backend.Generators.Shared.Dtos;

//namespace gAPI.CodeGen.Backend.Generators.Business.Mappings;

//public class MappingGenerator : BaseGenerator
//{
//    public MappingGenerator(DtoGenerator dto, DirectoryInfo directory, string @namespace)
//    {
//        Dto = dto;
//        DbSet = dto.DbSet;
//        Entity = DbSet.Entity;

//        Directory = directory;
//        Namespace = @namespace;
//        Name = $"{Entity.Name}Mapping";
//        FileName = $"{Name}.cs";
//    }

//    public DtoGenerator Dto { get; }
//    public DbSet DbSet { get; }
//    public Entity Entity { get; }
//    public void GenerateCode()
//    {
//        Reg("System.Linq.Expressions");
//        Reg("Microsoft.EntityFrameworkCore");
//        Reg("gAPI.Helpers");

//        var toNameCode = string.Empty;
//        var projectToNameCode = string.Empty;
//        foreach (var property in Entity.Properties)
//        {
//            if (property.IsHidden) continue;
//            if (property.IsLijst) continue;
//            if (property.NavigationDbSet != null)
//            {
//                var foreignDbSet = property.NavigationDbSet;
//                var foreignEntity = foreignDbSet.Entity;
//                var foreignNameProperties = foreignEntity.Properties.Where(a => a.IsName != null);
//                if (!foreignNameProperties.Any()) continue;

//                var values = foreignNameProperties
//                    .Select(a => $"source.{property.Name}?.{a.Name}{(a.IsNullable ? "?" : "")}{(a.TypeSimpleName != "string" ? ".ToString()" : "")}")
//                    .ToArray();
//                var value = $"{string.Join(" + \" \" + ", values)}";

//                toNameCode += $"        dest.{property.Name}Name = {value};\r\n";

//                projectToNameCode += $"                {property.Name}Name = {value.Replace("?", "")},\r\n";

//                continue;
//            }
//        }

//        var toDtoCode = string.Empty;
//        var toEntityCode = string.Empty;
//        var projectToCode = string.Empty;

//        foreach (var property in Entity.Properties)
//        {
//            if (property.IsHidden) continue;
//            if (property.IsLijst) continue;
//            if (property.NavigationDbSet != null) continue;

//            toDtoCode += $"        if (dest.{property.Name} != source.{property.Name}) {{ dest.{property.Name} = source.{property.Name}; dirty = true; }}\r\n";

//            if (property.IsReadOnly) continue;

//            toEntityCode += $"        if (dest.{property.Name} != source.{property.Name}) {{ dest.{property.Name} = source.{property.Name}; dirty = true; }}\r\n";

//            projectToCode += $"                {property.Name} = source.{property.Name},\r\n";
//        }

//        Code = GetNamespacesCode();

//        Code += $"namespace {Namespace};\r\n";
//        Code += $"\r\n";
//        Code += $"public static class {Name}\r\n";
//        Code += $"{{\r\n";

//        Code += $"    public static {Dto.FullName} ToDto(this {Entity.FullName} item)\r\n";
//        Code += $"    {{\r\n";
//        Code += $"        var newItem = new {Dto.FullName}();\r\n";
//        Code += $"        item.CopyTo(newItem);\r\n";
//        Code += $"        return newItem;\r\n";
//        Code += $"    }}\r\n\r\n";

//        Code += $"    public static {Entity.FullName} ToEntity(this {Dto.FullName} item)\r\n";
//        Code += $"    {{\r\n";
//        Code += $"        var newItem = new {Entity.FullName}();\r\n";
//        Code += $"        item.CopyTo(newItem);\r\n";
//        Code += $"        return newItem;\r\n";
//        Code += $"    }}\r\n\r\n";

//        Code += $"    public static bool CopyTo(this {Entity.FullName} source, {Dto.FullName} dest)\r\n";
//        Code += $"    {{\r\n";
//        Code += toNameCode;
//        Code += $"        var dirty = false;\r\n";
//        Code += toDtoCode;
//        Code += $"        return dirty;\r\n";
//        Code += $"    }}\r\n\r\n";

//        Code += $"    public static bool CopyTo(this {Dto.FullName} source, {Entity.FullName} dest)\r\n";
//        Code += $"    {{\r\n";
//        Code += $"        var dirty = false;\r\n";
//        Code += toEntityCode;
//        Code += $"        return dirty;\r\n";
//        Code += $"    }}\r\n\r\n";

//        Code += $"#nullable disable\r\n";

//        Code += GenerateProjectToDtos();

//        Code += $"}}";

//        Save();
//    }

//    private string GenerateProjectToDtos()
//    {
//        var context = new NameContext()
//        {
//            DtoNameProperties = Entity.Properties
//                .Where(a =>
//                    a.IsHidden == false &&
//                    a.IsLijst == false &&
//                    a.NavigationDbSet != null)
//                .Select(a => new DtoNameProperty()
//                {
//                    NameProperty = a,
//                    ForeignEntityNameProperties = a.NavigationDbSet!.Entity.Properties
//                        .Where(a => a.IsName != null)
//                        .ToArray()
//                })
//                .ToArray(),
//            MatchedDtoProperties = Entity.Properties
//                .Where(a =>
//                    a.IsHidden == false &&
//                    a.IsLijst == false &&
//                    a.NavigationDbSet == null)
//        };

//        return $@"
//    public static IQueryable<{Dto.FullName}> ProjectToDtos(this IQueryable<{Entity.FullName}> query, string[] orderby = null)
//    {{
//        orderby = orderby ?? Array.Empty<string>();
//        return query
//            .AsNoTracking()
//            .Select(source => new
//            {{{string.Join("",
//context.MatchedDtoProperties
//    .Select(a => $@"
//                source.{a.Name},"))}{string.Join("",
//context.DtoNameProperties
//    .Select(a => $@"
//                {a.Name}Name = 
//                    {string.Join(" + \" \" + \r\n                        ",
//    a.ForeignEntityNameProperties
//        .Select(b => b.IsName.Format($"source.{a.Name}.{b.Name}"))
//)},"))}
//            }})
//            .ApplyOrderBy(orderby);
//    }}
//";
//    }
//}