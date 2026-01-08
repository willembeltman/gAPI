using System.Linq;
using System;
using gAPI.AutoSse.Models;

namespace gAPI.AutoSse.Generators
{
    internal class ISseServiceContextGenerator : BaseGenerator
    {
        internal ISseServiceContextGenerator(
            ServiceContext dataModel,
            SseServiceGenerator sseService)
        {
            DataModel = dataModel;
            SseService = sseService;
            ISseService = sseService.Interface;

            Directory = dataModel.Config.SseServiceInterfaces_Destination.Directory;
            Namespace = dataModel.Config.SseServiceInterfaces_Destination.Namespace;

            Name = SseService.Interface.Name + "Context";
            FileName = $"{Name}.g.cs";
        }

        public ServiceContext DataModel { get; }
        public SseServiceGenerator SseService { get; }
        public Interface ISseService { get; }

        public void GenerateCode()
        {
            Reg(ISseService);
            Reg(SseService);
            Code = @$"{GetNamespacesCode()}#nullable enable

namespace {Namespace};

public interface {Name}
{{
    {ISseService.Name} ToAll {{ get; }}
    {ISseService.Name} ToUser(string userId);
    {ISseService.Name} ToSession(string sessionId);
}}
";
        }
    }
}