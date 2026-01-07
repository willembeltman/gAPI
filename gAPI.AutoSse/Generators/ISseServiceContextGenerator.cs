using System.Linq;
using System;
using gAPI.AutoSse.Models;

namespace gAPI.AutoSse.Generators
{
    internal class ISseServiceContextGenerator : BaseGenerator
    {
        internal ISseServiceContextGenerator(
            ServiceContext dataModel,
            SseServiceGenerator clientHandler)
        {
            DataModel = dataModel;
            ClientHandler = clientHandler;
            IClientHandler = ClientHandler.Interface;

            Directory = dataModel.Config.SseServices_Destination.Directory;
            Namespace = dataModel.Config.SseServices_Destination.Namespace;

            Name = "I" + ClientHandler.Interface.ApiName + "Context";
            FileName = $"{Name}.g.cs";
        }

        public ServiceContext DataModel { get; }
        public SseServiceGenerator ClientHandler { get; }
        public Interface IClientHandler { get; }

        public void GenerateCode()
        {
            Code = "";
            return;
            Reg(IClientHandler);
            Reg(ClientHandler);
            Code = @$"{GetNamespacesCode()}#nullable enable

namespace {Namespace};

public interface {Name}
{{
    {IClientHandler.Name} ToAll {{ get; }}
    {IClientHandler.Name} ToUser(object userId);
}}
";
        }
    }
}