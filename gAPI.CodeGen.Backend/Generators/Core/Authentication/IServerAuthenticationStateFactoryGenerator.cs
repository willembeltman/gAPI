//using gAPI.CodeGen.Backend.Models;

//namespace gAPI.CodeGen.Backend.Generators.Core.Authentication;

//public class IServerAuthenticationStateFactoryGenerator : BaseGenerator
//{
//    public IServerAuthenticationStateFactoryGenerator(
//        BackendGenerator context)
//    {
//        Directory = context.Config.Core_AuthenticationDirectory;
//        Namespace = context.Config.Core_AuthenticationNamespace;

//        Context = context;

//        Name = "IServerAuthenticationStateFactory";
//        FileName = $"{Name}.cs";
//    }

//    public BackendGenerator Context { get; }

//    public SharedReference ServerAuthenticationState => Context.ServerAuthenticationState;
//    public SharedReference AuthenticationHeaders => Context.SharedReferences.AuthenticationHeaders;
//    public SharedReference State => Context.State;

//    public void GenerateCode()
//    {
//        Reg(ServerAuthenticationState);
//        Reg(AuthenticationHeaders);
//        Reg(State);

//        Code = $@"{GetNamespacesCode()}
//namespace {Namespace};

//public interface {Name}
//{{
//    Task<{ServerAuthenticationState}> CreateAuthenticationStateAsync({AuthenticationHeaders} headers, {State}? stateData, CancellationToken ct);
//}}";
//        Save(false);
//    }
//}