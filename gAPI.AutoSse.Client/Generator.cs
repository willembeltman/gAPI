using gAPI.AutoSse.Client.Generators;
using gAPI.AutoSse.Client.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.IO;
using System.Text;

namespace gAPI.AutoSse.Client;

public class Generator
{
    public Generator(ServiceContext serviceContext, SharedReferences sharedReferences)
    {
        ServiceContext = serviceContext;
        SharedReferences = sharedReferences;

        //SseClient = new SseClientGenerator(this);
        SseManager = new ClientConnectionGenerator(this);
        AutoSseExtension = new AddAutoSseExtensionGenerator(this);
    }

    public ServiceContext ServiceContext { get; }
    public SharedReferences SharedReferences { get; }

    //public SseClientGenerator SseClient { get; }
    public ClientConnectionGenerator SseManager { get; }
    public AddAutoSseExtensionGenerator AutoSseExtension { get; }

    public void Generate(SourceProductionContext spc)
    {
        //SseClient.GenerateCode();
        //spc.AddSource(Path.Combine(SseClient.Directory, SseClient.FileName), SourceText.From(SseClient.Code, Encoding.UTF8));

        SseManager.GenerateCode();
        spc.AddSource(Path.Combine(SseManager.Directory, SseManager.FileName), SourceText.From(SseManager.Code, Encoding.UTF8));

        AutoSseExtension.GenerateCode();
        spc.AddSource(Path.Combine(AutoSseExtension.Directory, AutoSseExtension.FileName), SourceText.From(AutoSseExtension.Code, Encoding.UTF8));
    }
}