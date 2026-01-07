using System.Linq;

namespace gAPI.AutoSse.Generators
{
    internal class SseContextGenerator : BaseGenerator
    {
        internal SseContextGenerator(
            ServiceContext dataModel,
            SseServiceContextGenerator[] clientHandlerContexts,
            ISseContextGenerator iSignalRContext)
        {
            DataModel = dataModel;
            ClientHandlerContexts = clientHandlerContexts;
            ISignalRContext = iSignalRContext;

            Directory = dataModel.Config.SseServices_Destination.Directory;
            Namespace = dataModel.Config.SseServices_Destination.Namespace;

            Name = "SignalRContext";
            FileName = $"{Name}.g.cs";
        }

        public ServiceContext DataModel { get; }
        public SseServiceContextGenerator[] ClientHandlerContexts { get; }
        public ISseContextGenerator ISignalRContext { get; }

        public void GenerateCode()
        {
            Code = "";
            return;
            Reg(ISignalRContext);
            Reg("Microsoft.AspNetCore.SignalR");
            var properties = string.Join(
                Environment.NewLine,
                ClientHandlerContexts
                    .Select(a =>
                    {
                        Reg(a);
                        Reg(a.IClientHandlerContext);
                        return $"    public {a.IClientHandlerContext.Name} {a.ClientHandler.Interface.ApiName} {{ get; }} = new {a.Name}(sseContext);";
                    }));

            Code = @$"{GetNamespacesCode()}#nullable enable

namespace {Namespace};

public class {Name}(
    ISseContext<SignalRSse> sseContext)
    : {ISignalRContext.Name}
{{
{properties}
}}
";
        }
    }
}