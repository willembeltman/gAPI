//namespace gAPI.CodeGen.Backend.Generators.Core.Authentication;

//public class RequestIdsGenerator : BaseGenerator
//{
//    public RequestIdsGenerator(
//        BackendGenerator context)
//    {
//        Directory = context.Config.Core_AuthenticationDirectory;
//        Namespace = context.Config.Core_AuthenticationNamespace;

//        Name = "RequestIds";
//        FileName = $"{Name}.cs";
//    }

//    public void GenerateCode()
//    {
//        Code = $@"namespace {Namespace};

//public sealed class {Name}
//{{
//    public long SessionId {{ get; set; }}
//    public long RouteId {{ get; set; }}
//    public long UserIpId {{ get; set; }}
//    public long UserIpSessionId {{ get; set; }}
//    public long UserIpSessionTokenId {{ get; set; }}
//    public long UserIpSessionTokenRouteId {{ get; set; }}
//    public long UserIpSessionTokenRouteRequestId {{ get; set; }}
//    public int Counter {{ get; set; }}
//}}";
//        Save(false);
//    }
//}
