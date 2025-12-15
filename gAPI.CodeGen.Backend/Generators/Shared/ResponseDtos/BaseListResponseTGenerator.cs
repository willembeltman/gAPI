namespace gAPI.CodeGen.Backend.Generators.Shared.ResponseDtos;

public class BaseListResponseTGenerator : BaseGenerator
{
    public BaseListResponseTGenerator(
        BackendGenerator context,
        DirectoryInfo? businessServicesDirectory,
        string? businessServicesNamespace)
    {
        Context = context;
        BaseResponse = Context.BaseResponse;

        Directory = businessServicesDirectory;
        Namespace = businessServicesNamespace;

        Name = "BaseListResponseT";
        FileName = $"{Name}.cs";
    }

    public BackendGenerator Context { get; }
    public BaseResponseGenerator BaseResponse { get; }

    internal void GenerateCode()
    {
        Reg(BaseResponse);
        Reg("gAPI.Attributes");

        Code = $@"{GetNamespacesCode()}namespace {Namespace};

[IsBaseListResponseT]
public class {Name}<T> : {BaseResponse.Name}
{{
    public IAsyncEnumerable<T>? Response {{ get; set; }}
    public int Skip {{ get; set; }}
    public int Take {{ get; set; }}
    public int Total {{ get; set; }}
    public bool CanCreate {{ get; set; }}
}}";

        Save();
    }
}