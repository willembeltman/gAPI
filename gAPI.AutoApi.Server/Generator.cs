using gAPI.AutoApi.Server.Generators;
using gAPI.AutoApi.Server.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.IO;
using System.Linq;
using System.Text;

namespace gAPI.AutoApi.Server;

public class Generator
{
    public Generator(ServiceContext serviceContext, SharedReferences sharedReferences)
    {
        ServiceContext = serviceContext;
        SharedReferences = sharedReferences;

        Apis = serviceContext.ApiInterfaces
            .Select(service => new Controller_Generator(this, service))
            .ToArray();
        MinimalApis = serviceContext.MinimalApiInterfaces
            .Select(service => new MinimalApi_Generator(this, service))
            .ToArray();

        AddAutoApiServices = new AddAutoApiServicesExtension_Generator(this);
        AddAutoApi = new AddAutoApiExtension_Generator(this);
    }

    public ServiceContext ServiceContext { get; }
    public SharedReferences SharedReferences { get; }
    public Controller_Generator[] Apis { get; }
    public MinimalApi_Generator[] MinimalApis { get; }
    public AddAutoApiServicesExtension_Generator AddAutoApiServices { get; }
    public AddAutoApiExtension_Generator AddAutoApi { get; }

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
