using gAPI.AutoSseClient.Generators;
using gAPI.AutoSseClient.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.IO;
using System.Text;

namespace gAPI.AutoSseClient;

internal class Generator
{
    internal static void Generate(ServiceContext dataModel, SourceProductionContext spc)
    {
        var Config = dataModel.Config;

        var SseClient = new SseClientGenerator(dataModel);
        SseClient.GenerateCode();
        var SseClientFullName = Path.Combine(Config.HubClients_Destination.Directory, SseClient.FileName);
        spc.AddSource(SseClientFullName, SourceText.From(SseClient.Code, Encoding.UTF8));

        var ISseManager = new ISseManagerGenerator(dataModel);
        ISseManager.GenerateCode();
        var signalRHubFullName = Path.Combine(Config.HubClients_Destination.Directory, ISseManager.FileName);
        spc.AddSource(signalRHubFullName, SourceText.From(ISseManager.Code, Encoding.UTF8));

        var SseManager = new SseManagerGenerator(dataModel, ISseManager, SseClient);
        SseManager.GenerateCode();
        var SseManagerFullName = Path.Combine(Config.HubClients_Destination.Directory, SseManager.FileName);
        spc.AddSource(SseManagerFullName, SourceText.From(SseManager.Code, Encoding.UTF8));
    }
}