using System.Linq;

namespace gAPI.AutoSseClient.Generators;

internal class ISseManagerGenerator : BaseGenerator
{
    internal ISseManagerGenerator(
        ServiceContext dataModel)
    {
        DataModel = dataModel;

        Directory = dataModel.Config.HubClients_Destination.Directory;
        Namespace = dataModel.Config.HubClients_Destination.Namespace;

        Name = "ISseManager";
        FileName = $"{Name}.g.cs";
    }

    public ServiceContext DataModel { get; }

    public void GenerateCode()
    {
        Reg(DataModel.ISseManagerBase);

        var properties = string.Join(
            Environment.NewLine,
            DataModel.Interfaces
                .Select(a =>
                {
                    Reg(a);
                    return @$"
        Task SubscribeAsync({a.Name} implementation);
        Task UnsubscribeAsync({a.Name} implementation);
";
                }));

        Code = @$"{GetNamespacesCode()}#nullable enable

namespace {Namespace}
{{
    public interface {Name} : {DataModel.ISseManagerBase.Name}
    {{{properties}
    }}
}}
";
    }
}