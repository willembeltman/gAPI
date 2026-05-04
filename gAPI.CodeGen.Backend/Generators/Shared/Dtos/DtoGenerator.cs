using gAPI.CodeGen.Backend.Generators.Core.CrudHandlers;
using gAPI.CodeGen.Backend.Generators.Core.CrudMappings;
using gAPI.CodeGen.Backend.Generators.Core.CrudServices;
using gAPI.CodeGen.Backend.Generators.Shared.Interfaces;
using gAPI.CodeGen.Backend.Models.Entities;
using gAPI.Enums;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace gAPI.CodeGen.Backend.Generators.Shared.Dtos;

public class DtoGenerator : BaseGenerator
{
    public DtoGenerator(
        BackendGenerator context,
        DbSet dbSet)
    {
        Directory = context.Config.Shared_DtosDirectory;
        Namespace = context.Config.Shared_DtosNamespace;

        Context = context;
        DbSet = dbSet;

        // Generate Service Handlers
        CrudUseCase = new CrudUseCasesGenerator(
            context,
            this);

        CrudMapping = new CrudMappingGenerator(
            context,
            this);

        // Generate Service Interfaces
        ServiceInterface = new CrudServiceInterfaceGenerator(
            context,
            this);

        // Generate Services
        Service = new CrudServiceGenerator(
            context,
            this);

        Name = Entity.Name;
        FileName = $"{Name}.cs";
    }

    public BackendGenerator Context { get; }
    public DbSet DbSet { get; }

    public Entity Entity => DbSet.Entity;

    public CrudUseCasesGenerator CrudUseCase { get; }
    public CrudMappingGenerator CrudMapping { get; }
    public CrudServiceInterfaceGenerator ServiceInterface { get; }
    public CrudServiceGenerator Service { get; }

    public void GenerateCode()
    {
        var propertiesCode = string.Empty;
        var properties = Entity.Properties
            .ToArray();

        Reg("gAPI.Attributes");
        Reg("gAPI.Interfaces");
        if (Entity.IsStorageFileUrlProperty)
        {
            Reg("gAPI.Storage");
        }

        propertiesCode += $"\r\nnamespace {Namespace};\r\n";
        propertiesCode += $"\r\n";
        if (Entity.IsJunctionTable)
        {
            var props = properties
                .Where(a => a.IsForeignKey)
                .ToArray();
            var left = props[0].ForeignKeyProperty!.NavigationDbSet!.Entity.Name;
            var right = props[1].ForeignKeyProperty!.NavigationDbSet!.Entity.Name;
            propertiesCode += $"[IsJunctionTable(typeof({left}), typeof({right}))]\r\n";
        }
        if (Entity.IsAuthorize)
        {
            propertiesCode += $"[IsAuthorized]\r\n";
        }
        if (Entity.IsUser)
        {
            propertiesCode += $"[IsUser]\r\n";
        }
        if (Entity.IsEntryPoint)
        {
            propertiesCode += $"[IsEntryPoint]\r\n";
        }
        propertiesCode += $"public class {Name} : ICrudEntity{(Entity.IsStorageFileUrlProperty ? ", IStorageFileDto" : "")}\r\n";
        propertiesCode += $"{{\r\n";

        foreach (var property in properties)
        {
            if (property.IsHidden) continue;
            if (property.IsLijst) continue;

            if (property.NavigationDbSet != null && property.NavigationItemProperty != null)
            {
                var foreignDbSet = property.NavigationDbSet;
                var foreignEntity = foreignDbSet.Entity;
                var anyForeignNameProperty = foreignEntity.Properties.Any(a => a.IsName != null);
                if (!anyForeignNameProperty) continue;

                propertiesCode += $"    [IsForeignName(nameof({property.NavigationItemProperty.Name}))]\r\n";
                propertiesCode += $"    public string? {property.Name}Name {{ get; set; }}\r\n";

                continue;
            }

            if (property.ForeignKeyProperty != null && property.ForeignKeyProperty.NavigationDbSet != null)
            {
                propertiesCode += $"    [IsForeignKey(typeof({property.ForeignKeyProperty.NavigationDbSet.Entity.Name}))]\r\n";
            }

            if (property.IsKey)
            {
                Reg("System.ComponentModel.DataAnnotations");
                propertiesCode += "    [Key]\r\n";
            }

            if (property.IsStateManaged != null)
            {
                if (property.IsStateManaged.IsString)
                    propertiesCode += $"    [IsStateManaged(\"{property.IsStateManaged.Name}\", {property.IsStateManaged.CheckForNull.ToString().ToLower()}, {property.IsStateManaged.UseValue.ToString().ToLower()}, {property.IsStateManaged.IsString.ToString().ToLower()})]\r\n";
                else if (property.IsStateManaged.UseValue)
                    propertiesCode += $"    [IsStateManaged(\"{property.IsStateManaged.Name}\", {property.IsStateManaged.CheckForNull.ToString().ToLower()}, {property.IsStateManaged.UseValue.ToString().ToLower()})]\r\n";
                else if (property.IsStateManaged.CheckForNull)
                    propertiesCode += $"    [IsStateManaged(\"{property.IsStateManaged.Name}\", {property.IsStateManaged.CheckForNull.ToString().ToLower()})]\r\n";
                else
                    propertiesCode += $"    [IsStateManaged(\"{property.IsStateManaged.Name}\")]\r\n";
            }

            if (!property.IsReadOnly && !property.IsNullable && property.TypeSimpleName == "string" && !property.ValidationAttributes.Any(a => a.GetType() == typeof(RequiredAttribute)))
            {
                Reg("System.ComponentModel.DataAnnotations");
                propertiesCode += "    [Required]\r\n";
            }

            if (property.IsReadOnly)
            {
                propertiesCode += "    [IsReadOnly]\r\n";
            }
            if (property.IsImmutable)
            {
                propertiesCode += "    [IsImmutable]\r\n";
            }

            if (property.IsName != null)
            {
                if (property.IsName.FormattingOption == FormattingOption.ToString &&
                    string.IsNullOrEmpty(property.IsName.Start) &&
                    string.IsNullOrEmpty(property.IsName.End))
                {
                    // Empty constructor
                    propertiesCode += $"    [IsName]\r\n";
                }
                else if (string.IsNullOrEmpty(property.IsName.Start))
                {
                    // End constructor
                    if (string.IsNullOrEmpty(property.IsName.End))
                        propertiesCode += $"    [IsName(FormattingOption.{property.IsName.FormattingOption})]\r\n";
                    else
                        propertiesCode += $"    [IsName(FormattingOption.{property.IsName.FormattingOption}, \"{property.IsName.End}\")]\r\n";
                }
                else
                {
                    // Start constructor
                    if (string.IsNullOrEmpty(property.IsName.End))
                        propertiesCode += $"    [IsName(\"{property.IsName.Start}\", FormattingOption.{property.IsName.FormattingOption})]\r\n";
                    else
                        propertiesCode += $"    [IsName(\"{property.IsName.Start}\", FormattingOption.{property.IsName.FormattingOption}, \"{property.IsName.End}\")]\r\n";
                }
            }

            if (property.IsPrimitiveTypeOrEnumOrValueType || property.IsNullable)
            {
                propertiesCode = CreateAttributes(propertiesCode, property);

                if (property.TypeSimpleName == "DateOnly")
                {
                    propertiesCode += $"    public {property.TypeSimpleName} {property.Name} {{ get; set; }} = DateOnly.FromDateTime(DateTime.Now);\r\n";
                }
                else if (property.TypeSimpleName == "TimeOnly")
                {
                    propertiesCode += $"    public {property.TypeSimpleName} {property.Name} {{ get; set; }} = TimeOnly.FromDateTime(DateTime.Now);\r\n";
                }
                else if (property.TypeSimpleName == "DateTime")
                {
                    propertiesCode += $"    public {property.TypeSimpleName} {property.Name} {{ get; set; }} = DateTime.Now;\r\n";
                }
                else
                {
                    Reg(property.Type);
                    propertiesCode += $"    public {property.TypeSimpleName} {property.Name} {{ get; set; }}\r\n";
                }
            }
            else
            {
                propertiesCode = CreateAttributes(propertiesCode, property);

                if (property.TypeSimpleName == "string")
                {
                    propertiesCode += $"    public {property.TypeSimpleName} {property.Name} {{ get; set; }} = string.Empty;\r\n";
                }
                else
                {
                    Reg(property.Type);
                    propertiesCode += $"    public {property.TypeSimpleName} {property.Name} {{ get; set; }} = new();\r\n";
                }
            }
        }

        if (Entity.IsStorageFileUrlProperty)
        {
            propertiesCode += $"    [IsReadOnly]\r\n";
            propertiesCode += $"    [IsStorageFileUrlProperty]\r\n";
            propertiesCode += $"    public string? StorageFileUrl {{ get; set; }}\r\n";
        }

        propertiesCode += $"    [IsReadOnly]\r\n";
        propertiesCode += $"    public bool CanUpdate {{ get; set; }}\r\n";
        propertiesCode += $"    [IsReadOnly]\r\n";
        propertiesCode += $"    public bool CanDelete {{ get; set; }}\r\n";

        var foreignEntity2 = DbSet.Entity;
        var foreignNameProperties = foreignEntity2.Properties.Where(a => a.IsName != null);
        if (foreignNameProperties.Any())
        {
            var values = foreignNameProperties
                .Select(a => $"{{{a.Name}{(a.IsName.StringFormat == null ? "" : $":{a.IsName.StringFormat}")}}}")
                .ToArray();
            var value = $"$\"{string.Join(" ", values)}\"";
            propertiesCode += $"    public override string ToString() => {value};\r\n";
        }

        propertiesCode += $"}}";


        Code = GetNamespacesCode() + propertiesCode;

        Save();
        DoMappings();
    }

    private void DoMappings()
    {
        CrudUseCase.GenerateCode();
        CrudMapping.GenerateCode();
        ServiceInterface.GenerateCode();
        Service.GenerateCode();
    }

    public string CreateAttributes(string propertiesCode, EntityProperty property)
    {
        foreach (var attr in property.ValidationAttributes)
        {
            var attrType = attr.GetType();
            var attrName = attrType.Name.Replace("Attribute", ""); // RequiredAttribute => Required
            var ctorArgs = new List<string>();
            var namedArgs = new List<string>();

            // 1. Constructorparameters ophalen (vereenvoudigd: kijk naar de constructor met de meeste parameters)
            var ctors = attrType.GetConstructors();
            var bestCtor = ctors.OrderByDescending(c => c.GetParameters().Length).FirstOrDefault();

            if (bestCtor != null)
            {
                foreach (var param in bestCtor.GetParameters())
                {
                    var value = attrType.GetProperty(param.Name!, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance)?.GetValue(attr);
                    if (value != null)
                        ctorArgs.Add(FormatValue(value));
                }
            }

            // 2. Extra properties (zoals ErrorMessage)
            var writableProps = attrType.GetProperties()
                .Where(p => p.CanWrite && p.Name != "TypeId");

            foreach (var prop in writableProps)
            {
                var value = prop.GetValue(attr);
                if (value != null && !ctorArgs.Contains(FormatValue(value))) // voorkom dubbele info
                {
                    namedArgs.Add($"{prop.Name} = {FormatValue(value)}");
                }
            }

            Reg("System.ComponentModel.DataAnnotations");
            var allArgs = ctorArgs.Concat(namedArgs);
            if (allArgs.Any())
            {
                // 3. Samenstellen
                var args = string.Join(", ", allArgs);
                propertiesCode += $"    [{attrName}({args})]\r\n";
            }
            else
            {
                propertiesCode += $"    [{attrName}]\r\n";
            }
        }

        return propertiesCode;
    }
#nullable disable
    string FormatValue(object value)
    {
        return value switch
        {
            string s => $"\"{s}\"",
            char c => $"'{c}'",
            bool b => b.ToString().ToLower(),
            Enum e => $"{e.GetType().Name}.{e}",
            null => "null",
            _ => value.ToString()
        };
    }

}