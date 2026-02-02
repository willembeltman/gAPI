using gAPI.CodeGen.Backend.Models;

namespace gAPI.CodeGen.Backend.Generators.Api;

public class AddCommenServicesExtentionGenerator : BaseGenerator
{
    public AddCommenServicesExtentionGenerator(
        BackendGenerator context)
    {
        Directory = context.Config.Api_Directory;
        Namespace = context.Config.Api_Namespace;
        
        Context = context;

        Name = "AddCommenServicesExtention";
        FileName = $"{Name}.cs";
    }

    public BackendGenerator Context { get; }

    public SharedReference ServerConfig => Context.SharedReferences.ServerConfig;

    public void GenerateCode()
    {
        Reg(ServerConfig);
        Code = $@"{GetNamespacesCode()}
namespace {Namespace};

public static class {Name}
{{
    public static IServiceCollection AddCommenServices(this IServiceCollection services, {ServerConfig} serverConfig)
    {{
        services.AddSingleton(serverConfig);
        services.AddSingleton(TimeProvider.System);
        return services;
    }}
}}";
        Save(false);
    }
}
