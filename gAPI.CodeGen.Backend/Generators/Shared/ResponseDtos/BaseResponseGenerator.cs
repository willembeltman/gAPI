using gAPI.CodeGen.Backend.Generators.Shared.Dtos;

namespace gAPI.CodeGen.Backend.Generators.Shared.ResponseDtos;

public class BaseResponseGenerator : BaseGenerator
{
    public BaseResponseGenerator(BackendGenerator context, DirectoryInfo responseDtoDirectory, string responseDtoNamespace)
    {
        Context = context;

        Directory = responseDtoDirectory;
        Namespace = responseDtoNamespace;

        Name = "BaseResponse";
        FileName = $"{Name}.cs";
    }

    public BackendGenerator Context { get; }
    public StateDtoGenerator StateDto => Context.StateDto;

    public void GenerateCode()
    {
        Reg(StateDto);
        Reg("gAPI.Attributes");

        Code = $@"{GetNamespacesCode()}namespace {Namespace};

[IsBaseResponse]
public class {Name} : gAPI.Dtos.BaseResponse
{{
    public {StateDto.Name} {StateDto.Name} {{ get; set; }} = new {StateDto.Name}();
}}";

        Save();
    }
}