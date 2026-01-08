using gAPI.Attributes;
using gAPI.CodeGen.Backend.Generators.Business.CrudHandlers;
using gAPI.CodeGen.Backend.Generators.Business.CrudMappings;
using gAPI.CodeGen.Backend.Generators.Business.CrudServices;
using gAPI.CodeGen.Backend.Generators.Business.Interfaces;
using gAPI.CodeGen.Backend.Generators.Shared.Interfaces;
using gAPI.CodeGen.Backend.Models.Entities;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace gAPI.CodeGen.Backend.Generators.Shared.Dtos;

public class DtoGenerator : BaseGenerator
{
    public DtoGenerator(
        BackendGenerator context,
        DbSet dbSet,
        DirectoryInfo dtoDirectory, string dtoNamespace,
        DirectoryInfo CrudHandlersDirectory, string CrudHandlersNamespace,
        DirectoryInfo CrudMappingsDirectory, string CrudMappingsNamespace,
        DirectoryInfo CrudServiceInterfacesDirectory, string CrudServiceInterfacesNamespace,
        DirectoryInfo CrudServicesDirectory, string CrudServicesNamespace)
    {
        Context = context;
        DbSet = dbSet;

        Directory = dtoDirectory;
        Namespace = dtoNamespace;

        // Generate Service Handlers
        Handler = new CrudHandlerGenerator(
            this,
            CrudHandlersDirectory,
            CrudHandlersNamespace);

        CustomMapping = new CrudMappingGenerator(
            Handler,
            CrudMappingsDirectory,
            CrudMappingsNamespace);

        // Generate Service Interfaces
        ServiceInterface = new CrudServiceInterfaceGenerator(
            CustomMapping,
            CrudServiceInterfacesDirectory,
            CrudServiceInterfacesNamespace);

        // Generate Services
        Service = new CrudServiceGenerator(
            ServiceInterface,
            CrudServicesDirectory,
            CrudServicesNamespace);

        Name = Entity.Name;
        FileName = $"{Name}.cs";
    }

    public BackendGenerator Context { get; }
    public DbSet DbSet { get; }

    public IServerAuthenticationServiceGenerator IServerAuthenticationService => Context.IServerAuthenticationService;
    public Entity Entity => DbSet.Entity;

    public CrudHandlerGenerator Handler { get; }
    public CrudMappingGenerator CustomMapping { get; }
    public CrudServiceInterfaceGenerator ServiceInterface { get; }
    public CrudServiceGenerator Service { get; }

    public void GenerateCode()
    {
        var propertiesCode = string.Empty;

        Reg("gAPI.Attributes");
        Reg("gAPI.Interfaces");
        if (Entity.IsStorageFile)
        {
            Reg("gAPI.Storage");
        }

        propertiesCode += $"namespace {Namespace};\r\n";
        propertiesCode += $"\r\n";
        if (Entity.IsJunctionTable)
        {
            var props = Entity.Properties
                .Where(a => a.IsForeignKey)
                .ToArray();
            var left = props[0].ForeignKey!.NavigationDbSet!.Entity.Name;
            var right = props[1].ForeignKey!.NavigationDbSet!.Entity.Name;
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
        propertiesCode += $"public class {Name} : ICrudEntity{(Entity.IsStorageFile ? ", IStorageFileDto" : "")}\r\n";
        propertiesCode += $"{{\r\n";

        foreach (var property in Entity.Properties)
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

            if (property.ForeignKey != null && property.ForeignKey.NavigationDbSet != null)
            {
                propertiesCode += $"    [IsForeignKey(typeof({property.ForeignKey.NavigationDbSet.Entity.Name}))]\r\n";
            }

            if (property.IsKey)
            {
                Reg("System.ComponentModel.DataAnnotations");
                propertiesCode += "    [Key]\r\n";
            }

            if (!Entity.IsUser && property.IsStateManaged && property.StateUserProperty != null)
            {
                propertiesCode += $"    [IsStateManaged(nameof({property.StateUserProperty.Entity.Name}.{property.StateUserProperty.Name}))]\r\n";
            }

            if (!property.IsReadOnly && !property.IsNullable && property.TypeSimpleName == "string" && !property.ValidationAttributes.Any(a => a.GetType() == typeof(RequiredAttribute)))
            {
                Reg("System.ComponentModel.DataAnnotations");
                propertiesCode += "    [Required]\r\n";
            }

            if (property.IsUnique)
            {
                propertiesCode += "    [IsUnique]\r\n";
            }

            if (property.IsReadOnly)
            {
                propertiesCode += "    [IsReadOnly]\r\n";
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

        if (Entity.IsStorageFile)
        {
            propertiesCode += $"    [IsReadOnly]\r\n";
            propertiesCode += $"    [IsStorageFile]\r\n";
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

        //if (!Entity.IsUser)

        Handler.GenerateCode();
        CustomMapping.GenerateCode();
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