using System.Linq;

namespace gAPI.AutoHubClient.Generators
{
    internal class ISignalRConnectionGenerator : BaseGenerator
    {
        internal ISignalRConnectionGenerator(
            ServiceContext dataModel)
        {
            DataModel = dataModel;

            Directory = dataModel.Config.HubClients_Destination.Directory;
            Namespace = dataModel.Config.HubClients_Destination.Namespace;

            Name = "ISignalRConnection";
            FileName = $"{Name}.g.cs";
        }

        public ServiceContext DataModel { get; }

        public void GenerateCode()
        {
            var properties = string.Join(
                Environment.NewLine,
                DataModel.Interfaces
                    .Select(a =>
                    {
                        Reg(a);
                        return @$"
    Task RegisterEventHandlerAsync({a.Name} implementation);
    void UnRegisterEventHandlerAsync({a.Name} implementation);
";
                    }));
            Code = @$"{GetNamespacesCode()}#nullable enable

using BSD.Shared;

namespace {Namespace};

public interface {Name} : IAsyncDisposable
{{{properties}
}}
";
        }
    }
}