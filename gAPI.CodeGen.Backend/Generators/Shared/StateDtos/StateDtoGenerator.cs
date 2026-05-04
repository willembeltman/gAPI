using gAPI.CodeGen.Backend.Models;
using gAPI.CodeGen.Backend.Models.Entities;
using gAPI.Storage;

namespace gAPI.CodeGen.Backend.Generators.Shared.StateDtos;

public class StateDtoGenerator : BaseGenerator
{
    public StateDtoGenerator(
        BackendGenerator context,
        StateObject stateObject)
    {
        Directory = context.Config.Shared_StateDtosDirectory;
        Namespace = context.Config.Shared_StateDtosNamespace;

        Context = context;
        StateObject = stateObject;

        IsUser = context.DbContext.StateUser == stateObject;

        Properties = stateObject.KeyProperties.Select(a => new StateDtoPropertyGenerator(this, a, true))
            .Concat(stateObject.ForeignProperties.Select(a => new StateDtoPropertyGenerator(this, a)))
            .Concat(stateObject.Properties.Select(a => new StateDtoPropertyGenerator(this, a)))
            .Concat(stateObject.ForeignLists.Select(a => new StateDtoPropertyGenerator(this, a, true)))
            .ToArray();
        IsStorageFileUrlProperty = stateObject.Entity.IsStorageFileUrlProperty;

        Name = "State" + stateObject.Entity.Name;
        FileName = $"{Name}.cs";
    }

    public BackendGenerator Context { get; }
    public StateObject StateObject { get; }
    public bool IsUser { get; }
    public StateDtoPropertyGenerator[] Properties { get; }
    public bool IsStorageFileUrlProperty { get; }

    public Entity Entity => StateObject.Entity;

    public SharedReference IsStorageFileUrlPropertyAttribute => Context.SharedReferences.IsStorageFileUrlProperty;
    public SharedReference IStorageFileDtoAttribute => Context.SharedReferences.IStorageFileDto;

    public void GenerateCode()
    {
        if (IsStorageFileUrlProperty)
        {
            Reg(IsStorageFileUrlPropertyAttribute);
            Reg(IStorageFileDtoAttribute);
        }

        Reg("System.ComponentModel.DataAnnotations");
        foreach (var prop in Properties)
        {
            var stateDto = Context.AllStateObjects.FirstOrDefault(a => a.Entity?.FullName == prop.Entity?.FullName);
            Reg(stateDto);
        }
        var properties = string.Join("", Properties.Select(GeneratePropertyCode));

        Code = $@"{GetNamespacesCode()}
namespace {Namespace};

public class {Name}{(IsStorageFileUrlProperty ? $" : {IStorageFileDtoAttribute}" : "")}
{{
    {properties}{(IsStorageFileUrlProperty ? $@"
    [IsStorageFileUrlProperty]
    public string? StorageFileUrl {{ get; set; }}" : "")}
}}";
        if (!IsUser)
            Save();
    }


    public string GeneratePropertyCode(StateDtoPropertyGenerator a)
    {
        var code = string.Empty;
        if (a.IsKey)
        {
            code += $@"
    [Key]";
        }

        if (a.Entity == null)
        {
            var end = a.DataType == "string" ? " = string.Empty;" : "";
            code += $@"
    public {a.DataType} {a.Name} {{ get; set; }}{end}";
        }
        else
        {
            if (a.IsList)
            {
                code += $@"
    public IEnumerable<{a.DataType}> {a.Name} {{ get; set; }} = [];";
            }
            else
            {
                code += $@"
    public {a.DataType}? {a.Name} {{ get; set; }}";
            }
        }

        return code;
        //[Key]
        //public Guid Id {{ get; set; }}
        //public string UserName {{ get; set; }} = string.Empty;
        //public string Email {{ get; set; }} = string.Empty;
        //public StateCompany? CurrentCompany {{ get; set; }}
        //public IEnumerable<StateCompanyUser> CompanyUsers {{ get; set; }} = [];
    }
}

public class StateDtoPropertyGenerator
{
    public StateDtoPropertyGenerator(StateDtoGenerator dto, StateObject a, bool isList = false)
    {
        ParentDto = dto;
        IsKey = false;
        IsList = isList;
        DataType = "State" + a.Entity.Name;
        Name = a.PropertyOfParent!.Name;
        Entity = a.Entity;
    }
    public StateDtoPropertyGenerator(StateDtoGenerator dto, StateObjectProperty a, bool isKey = false)
    {
        ParentDto = dto;
        IsKey = isKey;
        IsList = false;
        DataType = a.Property.TypeSimpleName;
        Name = a.Property.Name;
    }

    public StateDtoGenerator ParentDto { get; }
    public bool IsKey { get; }
    public bool IsList { get; }
    public string DataType { get; }
    public string Name { get; }
    public Entity? Entity { get; }
}