using gAPI.AutoSseServer.Generators;
using gAPI.AutoSseServer.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.IO;
using System.Linq;
using System.Text;

namespace gAPI.AutoSseServer;

public class Generator
{
    public Generator(ServiceContext serviceContext, SharedReferences sharedReferences)
    {
        ServiceContext = serviceContext;
        SharedReferences = sharedReferences;

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

        AddAutoSseExtension = new AddAutoSseExtention_Generator(this);

    }

    public ServiceContext ServiceContext { get; }
    public SharedReferences SharedReferences { get; }

    public ClientService_Generator[] SseServices { get; }
    public IClientServiceContext_Generator[] IClientContexts { get; }
    public ClientServiceContext_Generator[] ClientContexts { get; }
    public IClientContext_Generator IClientContext { get; }
    public ClientContext_Generator ClientContext { get; }
    public AddAutoSseExtention_Generator AddAutoSseExtension { get; }

    public void Generate(SourceProductionContext spc)
    {
        //var SseHostController = new SseHostControllerGenerator(dataModel);
        //SseHostController.GenerateCode();
        //spc.AddSource(
        //    Path.Combine(SseHostController.Directory, SseHostController.FileName),
        //    SourceText.From(SseHostController.Code, Encoding.UTF8));

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

        AddAutoSseExtension.GenerateCode();
        spc.AddSource(
            Path.Combine(AddAutoSseExtension.Directory, AddAutoSseExtension.FileName),
            SourceText.From(AddAutoSseExtension.Code, Encoding.UTF8));
    }
}