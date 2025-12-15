//using gAPI.CodeGen.FrontEnd.Config;

//namespace gAPI.CodeGen.FrontEnd.Step2Generators.SingleFiles;

//public class ClientAuthenticationServiceGenerator : BaseGenerator
//{
//    public ClientAuthenticationServiceGenerator(IClientAuthenticationServiceGenerator iClientAuthenticationService)
//    {
//        IClientAuthenticationService = iClientAuthenticationService;
//        Config = iClientAuthenticationService.Config;

//        Directory = Config.DotNetClientServicesDirectory;
//        Namespace = Config.DotNetClientServicesNamespace;

//        Name = "ClientAuthenticationService";
//        FileName = $"{Name}.g.cs";
//    }

//    public IClientAuthenticationServiceGenerator IClientAuthenticationService { get; }
//    public Step2GeneratorConfig Config { get; }

//    public void GenerateCode()
//    {
//    }
//}