namespace gAPI.CodeGen.Backend.Generators.Shared.ResponseDtos
{
    public class BaseResponseTGenerator : BaseGenerator
    {
        public BaseResponseTGenerator(BackendGenerator context, DirectoryInfo responseDtoDirectory, string responseDtoNamespace)
        {
            Context = context;
            BaseResponse = context.BaseResponse;

            Directory = responseDtoDirectory;
            Namespace = responseDtoNamespace;

            Name = "BaseResponseT";
            FileName = $"{Name}.cs";
        }

        public BackendGenerator Context { get; }
        public BaseResponseGenerator BaseResponse { get; }

        public void GenerateCode()
        {
            Reg(BaseResponse);
            Reg("gAPI.Attributes");

            Code = $@"{GetNamespacesCode()}namespace {Namespace};

[IsBaseResponseT]
public class {Name}<T> : {BaseResponse.Name}
{{
    public T? Response {{ get; set; }}
}}";

            Save();
        }
    }
}