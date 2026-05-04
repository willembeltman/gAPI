//using gAPI.CodeGen.Backend.Models;

//namespace gAPI.CodeGen.Backend.Generators.Api;

//public class AddRemainingAuthenticationServicesExtensionGenerator : BaseGenerator
//{
//    public AddRemainingAuthenticationServicesExtensionGenerator(
//        BackendGenerator context)
//    {
//        Directory = context.Config.Api_Directory;
//        Namespace = context.Config.Api_Namespace;

//        Context = context;

//        Name = "AddRemainingAuthenticationServicesExtension";
//        FileName = $"{Name}.cs";
//    }

//    public BackendGenerator Context { get; }

//    public SharedReference IServerAuthenticationSecurity => Context.IServerAuthenticationSecurity;
//    public SharedReference ServerAuthenticationSecurity => Context.ServerAuthenticationSecurity;
//    public SharedReference IServerAuthenticationStateFactory => Context.IServerAuthenticationStateFactory;
//    public SharedReference ServerAuthenticationStateFactory => Context.ServerAuthenticationStateFactory;
//    public SharedReference StateMapping => Context.StateMapping;

//    public void GenerateCode()
//    {
//        Reg(IServerAuthenticationSecurity);
//        Reg(ServerAuthenticationSecurity);
//        Reg(IServerAuthenticationStateFactory);
//        Reg(ServerAuthenticationStateFactory);
//        Reg(StateMapping);

//        Code = $@"{GetNamespacesCode()}
//namespace {Namespace};

//public static class AddRemainingAuthenticationServicesExtension
//{{
//    public static IServiceCollection AddRemainingAuthenticationServices(this IServiceCollection services)
//    {{
//        services.AddScoped<{IServerAuthenticationSecurity}, {ServerAuthenticationSecurity}>();
//        services.AddScoped<{IServerAuthenticationStateFactory}, {ServerAuthenticationStateFactory}>();
//        services.AddScoped<{StateMapping}>();
//        return services;
//    }}
//}}";
//        Save(false);
//    }
//}
