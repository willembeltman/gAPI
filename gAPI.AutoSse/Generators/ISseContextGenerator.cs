using System.Linq;

namespace gAPI.AutoSse.Generators
{
    internal class ISseContextGenerator : BaseGenerator
    {
        internal ISseContextGenerator(
            ServiceContext dataModel,
            SseServiceContextGenerator[] clientHandlerContexts)
        {
            DataModel = dataModel;
            ClientHandlerContexts = clientHandlerContexts;

            Directory = dataModel.Config.SseContexts_Destination.Directory;
            Namespace = dataModel.Config.SseContexts_Destination.Namespace;

            Name = "ISseContext";
            FileName = $"{Name}.g.cs";
        }

        public ServiceContext DataModel { get; }
        public SseServiceContextGenerator[] ClientHandlerContexts { get; }

        public void GenerateCode()
        {
            Code = "";
            return;
            var properties = string.Join(
                Environment.NewLine,
                ClientHandlerContexts
                    .Select(a =>
                    {
                        Reg(a.IClientHandlerContext);
                        return $"    {a.IClientHandlerContext.Name} {a.ClientHandler.Interface.ApiName} {{ get; }}";
                    }));
            Code = @$"{GetNamespacesCode()}#nullable enable

namespace {Namespace};

public interface {Name}
{{
{properties}
}}
";
        }
    }
}