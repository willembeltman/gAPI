//using gAPI.AutoComponent.Interfaces;

//namespace gAPI.AutoComponent.Generators.Helpers;

//public class IClientAuthenticatedHttpClientGenerator : BaseGenerator
//{
//    public IClientAuthenticatedHttpClientGenerator(
//        ISharedReference state,
//        ISharedReference stateChangedHandler,
//        string directory,
//        string @namespace) 
//    {
//        State = state;
//        StateChangedHandler = stateChangedHandler;

//        Directory = directory;
//        Namespace = @namespace;

//        Name = "IClientAuthenticatedHttpClient";
//        FileName = $"{Name}.g.cs";
//    }

//    public ISharedReference State { get; }
//    public ISharedReference StateChangedHandler { get; }

//    public void GenerateCode()
//    {
//        //Code = "";
//        //return;

//        Reg(State);
//        Reg(StateChangedHandler);

//        Code = $@"{GetNamespacesCode()}
//namespace {Namespace};

//public interface {Name} : gAPI.Interfaces.IClientAuthenticatedHttpClient
//{{
//    event {StateChangedHandler}? OnStateHasChanged;
//    Task<{State}> GetStateAsync(CancellationToken ct);
//}}";
//    }
//}
