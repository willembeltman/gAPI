using gAPI.AutoApiSse.Server.Generators;
using gAPI.AutoApiSse.Server.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.IO;
using System.Linq;
using System.Text;

namespace gAPI.AutoApiSse.Server;

public class Generator
{
    public Generator(ServiceContext serviceContext, SharedReferences sharedReferences)
    {
        ServiceContext = serviceContext;
        SharedReferences = sharedReferences;

        // Api
        Apis = serviceContext.ApiInterfaces
            .Select(service => new Controller_Generator(this, service))
            .ToArray();
        MinimalApis = serviceContext.MinimalApiInterfaces
            .Select(service => new MinimalApi_Generator(this, service))
            .ToArray();

        // Sse
        SseServices = serviceContext.HubInterfaces
            .Select(@interface => new ClientService_Generator(this, @interface))
            .ToArray();

        IClientContexts = SseServices
            .Select(clientHandler => new IClientServiceContext_Generator(this, clientHandler))
            .ToArray();

        ClientContexts = IClientContexts
            .Select(clientHandler => new ClientServiceContext_Generator(this, clientHandler))
            .ToArray();

        IClientContext = new IClientContext_Generator(this);

        ClientContext = new ClientContext_Generator(this);


        AddAutoApi = new AddAutoApiSseServerExtension_Generator(this);
        SseEndpointExtension = new SseEndpointExtension_Generator(this);
    }

    public ServiceContext ServiceContext { get; }
    public SharedReferences SharedReferences { get; }
    public Controller_Generator[] Apis { get; }
    public MinimalApi_Generator[] MinimalApis { get; }
    public ClientService_Generator[] SseServices { get; }
    public IClientServiceContext_Generator[] IClientContexts { get; }
    public ClientServiceContext_Generator[] ClientContexts { get; }
    public IClientContext_Generator IClientContext { get; }
    public ClientContext_Generator ClientContext { get; }
    public AddAutoApiSseServerExtension_Generator AddAutoApi { get; }
    public SseEndpointExtension_Generator SseEndpointExtension { get; }

    public void Generate(SourceProductionContext spc)
    {
        // Api
        foreach (var api in Apis)
        {
            api.GenerateCode();
            spc.AddSource(Path.Combine(api.Directory, api.FileName), SourceText.From(api.Code, Encoding.UTF8));
        }
        foreach (var api in MinimalApis)
        {
            api.GenerateCode();
            spc.AddSource(Path.Combine(api.Directory, api.FileName), SourceText.From(api.Code, Encoding.UTF8));
        }

        // Sse
        foreach (var clientHandler in SseServices)
        {
            clientHandler.GenerateCode();
            spc.AddSource(
                Path.Combine(clientHandler.Directory, clientHandler.FileName),
                SourceText.From(clientHandler.Code, Encoding.UTF8));
        }

        foreach (var iClientHandlerContext in IClientContexts)
        {
            iClientHandlerContext.GenerateCode();
            spc.AddSource(
                Path.Combine(iClientHandlerContext.Directory, iClientHandlerContext.FileName),
                SourceText.From(iClientHandlerContext.Code, Encoding.UTF8));
        }
        foreach (var clientHandlerContext in ClientContexts)
        {
            clientHandlerContext.GenerateCode();
            spc.AddSource(
                Path.Combine(clientHandlerContext.Directory, clientHandlerContext.FileName),
                SourceText.From(clientHandlerContext.Code, Encoding.UTF8));
        }

        IClientContext.GenerateCode();
        spc.AddSource(
            Path.Combine(IClientContext.Directory, IClientContext.FileName),
            SourceText.From(IClientContext.Code, Encoding.UTF8));

        ClientContext.GenerateCode();
        spc.AddSource(
            Path.Combine(ClientContext.Directory, ClientContext.FileName),
            SourceText.From(ClientContext.Code, Encoding.UTF8));

        // Add
        AddAutoApi.GenerateCode();
        spc.AddSource(Path.Combine(AddAutoApi.Directory, AddAutoApi.FileName), SourceText.From(AddAutoApi.Code, Encoding.UTF8));

        SseEndpointExtension.GenerateCode();
        spc.AddSource(Path.Combine(SseEndpointExtension.Directory, SseEndpointExtension.FileName), SourceText.From(SseEndpointExtension.Code, Encoding.UTF8));

    }
}