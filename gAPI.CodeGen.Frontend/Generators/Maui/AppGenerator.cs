//using gAPI.CodeGen.FrontEnd.Config;

//namespace gAPI.CodeGen.FrontEnd.Step2Generators.SingleFiles;

//public class ClientAuthenticatedHttpClientGenerator : BaseGenerator
//{
//    public ClientAuthenticatedHttpClientGenerator(IClientAuthenticatedHttpClientGenerator iClientAuthenticatedHttpClient)
//    {
//        IClientAuthenticatedHttpClient = iClientAuthenticatedHttpClient;
//        Config = iClientAuthenticatedHttpClient.Config;

//        Directory = Config.DotNetClientServicesDirectory;
//        Namespace = Config.DotNetClientServicesNamespace;

//        Name = "ClientAuthenticatedHttpClient";
//        FileName = $"{Name}.g.cs";
//    }

//    public IClientAuthenticatedHttpClientGenerator IClientAuthenticatedHttpClient { get; }
//    public Step2GeneratorConfig Config { get; }

//    public void GenerateCode()
//    {
//    }
//}