using gAPI.AutoApiServer.Generators;
using gAPI.AutoApiServer.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.IO;
using System.Linq;
using System.Text;

namespace gAPI.AutoApiServer;

public class Generator
{
    public Generator(ServiceContext serviceContext, SharedReferences sharedReferences)
    {
        ServiceContext = serviceContext;
        SharedReferences = sharedReferences;

        Apis = serviceContext.ApiInterfaces
            .Select(service => new ControllerGenerator(this, service))
            .ToArray();
        MinimalApis = serviceContext.MinimalApiInterfaces
            .Select(service => new MinimalApiGenerator(this, service))
            .ToArray();

        AddAutoApiServices = new AddAutoApiServicesExtensionGenerator(this);
        AddAutoApi = new AddAutoApiExtensionGenerator(this);
    }

    public ServiceContext ServiceContext { get; }
    public SharedReferences SharedReferences { get; }
    public ControllerGenerator[] Apis { get; }
    public MinimalApiGenerator[] MinimalApis { get; }
    public AddAutoApiServicesExtensionGenerator AddAutoApiServices { get; }
    public AddAutoApiExtensionGenerator AddAutoApi { get; }

    public void Generate(SourceProductionContext spc)
    {
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

        AddAutoApiServices.GenerateCode();
        spc.AddSource(Path.Combine(AddAutoApiServices.Directory, AddAutoApiServices.FileName), SourceText.From(AddAutoApiServices.Code, Encoding.UTF8));

        AddAutoApi.GenerateCode();
        spc.AddSource(Path.Combine(AddAutoApi.Directory, AddAutoApi.FileName), SourceText.From(AddAutoApi.Code, Encoding.UTF8));
    }
}
