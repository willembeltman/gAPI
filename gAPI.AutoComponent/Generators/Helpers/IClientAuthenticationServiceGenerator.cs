using gAPI.AutoComponent.Interfaces;

namespace gAPI.AutoComponent.Generators.Helpers
{
    public class IClientAuthenticationServiceGenerator : BaseGenerator
    {
        public IClientAuthenticationServiceGenerator(
            ISharedReference state,
            StateChangedHandlerGenerator stateChangedHandler,
            string directory,
            string @namespace) : base(directory, @namespace)
        {
            State = state;
            StateChangedHandler = stateChangedHandler;
            Name = "IClientAuthenticationService";
            FileName = $"{Name}.g.cs";
        }

        public ISharedReference State { get; }
        public StateChangedHandlerGenerator StateChangedHandler { get; }

        public void GenerateCode()
        {
            Reg(State);
            Reg(StateChangedHandler);

            Code = $@"{GetNamespacesCode()}

#nullable enable

namespace {Namespace}
{{
    public interface {Name} : gAPI.Interfaces.IClientAuthenticationService
    {{
        event {StateChangedHandler.Name}? OnStateHasChanged;
        Task<{State.Name}?> GetState(CancellationToken? ct = null);
    }}
}}
";
        }
    }
}
