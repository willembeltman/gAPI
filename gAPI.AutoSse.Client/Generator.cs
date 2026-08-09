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
        ClientConnection = new ClientConnectionGenerator(this);
        IClientConnection = new IClientConnectionGenerator(this);
        AutoSseExtension = new AddAutoSseExtensionGenerator(this);
    }

    public ServiceContext ServiceContext { get; }
    public SharedReferences SharedReferences { get; }

    //public SseClientGenerator SseClient { get; }
    public ClientConnectionGenerator ClientConnection { get; }
    public IClientConnectionGenerator IClientConnection { get; }
    public AddAutoSseExtensionGenerator AutoSseExtension { get; }

    public void Generate(SourceProductionContext spc)
    {
        //SseClient.GenerateCode();
        //spc.AddSource(Path.Combine(SseClient.Directory, SseClient.FileName), SourceText.From(SseClient.Code, Encoding.UTF8));

        ClientConnection.GenerateCode();
        spc.AddSource(Path.Combine(ClientConnection.Directory, ClientConnection.FileName), SourceText.From(ClientConnection.Code, Encoding.UTF8));

        IClientConnection.GenerateCode();
        spc.AddSource(Path.Combine(IClientConnection.Directory, IClientConnection.FileName), SourceText.From(IClientConnection.Code, Encoding.UTF8));

        AutoSseExtension.GenerateCode();
        spc.AddSource(Path.Combine(AutoSseExtension.Directory, AutoSseExtension.FileName), SourceText.From(AutoSseExtension.Code, Encoding.UTF8));
    }
}