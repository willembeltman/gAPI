using gAPI.Attributes;
using gAPI.CodeGen.Frontend.Configs;
using gAPI.CodeGen.Frontend.Helpers;
using gAPI.CodeGen.Frontend.Models;
using gAPI.CodeGen.Frontend.Models.ServiceModels;
using System.Reflection;

namespace gAPI.CodeGen.Frontend.Contexts;

public class ServiceContext
{
    public ServiceContext(FrontendConfig config)
    {
        Config = config ?? throw new Exception("ServerConfig cannot be null");

        var allTypes =
            config.Assemblies.SelectMany(a => a.GetTypes());

        var interfaceTypes = allTypes
            .Where(t =>
                t.IsInterface &&
                t.GetCustomAttributes(typeof(GenerateApiAttribute), inherit: true).Length > 0
            )
            .ToArray();

        Interfaces = interfaceTypes
            .Select(interfaceType => new Interface(this, interfaceType, allTypes))
            .ToArray();

        var collector = new TypeCollector(config);

        foreach (var interfaceType in interfaceTypes)
        {
            // Alleen publieke methods ophalen
            var methods = interfaceType.GetMethods(
                BindingFlags.Public | BindingFlags.Instance
            );

            foreach (var method in methods)
            {
                // Parameters toevoegen
                foreach (var param in method.GetParameters())
                {
                    collector.Add(param.ParameterType);
                }

                // Returntype toevoegen
                collector.Add(method.ReturnType);
            }
        }


        Enums = collector.Enums
            .Select(namedTypeSymbol => new EnumDto(namedTypeSymbol))
            .ToArray();
        Dtos = collector.Dtos
            .Select(namedTypeSymbol => new Dto(this, namedTypeSymbol))
            .ToArray();

        IClientAuthenticationService = allTypes
            .Where(a => a.Name == "IClientAuthenticationService")
            .Select(a => new SharedReference(a))
            .FirstOrDefault() ?? throw new Exception("cannot find IClientAuthenticationService");

        ClientAuthenticationService = allTypes
            .Where(a => a.IsClass && a.GetInterface(IClientAuthenticationService.FullName) != null)
            .Select(a => new SharedReference(a))
            .FirstOrDefault() ?? throw new Exception(
                $"Could not find a service implementing `IClientAuthenticationService` inside your assembly, " +
                $"please create one.");



        ItemDataSource = allTypes
            .Where(a => a.Name == "ItemDataSource`2")
            .Select(a => new SharedReference(a))
            .FirstOrDefault() ?? throw new Exception("cannot find ItemDataSource");

        ListDataSource = allTypes
            .Where(a => a.Name == "ListDataSource`2")
            .Select(a => new SharedReference(a))
            .FirstOrDefault() ?? throw new Exception("cannot find ListDataSource");

        FormFile = allTypes
            .Where(t =>
                t.IsClass &&
                t.GetCustomAttribute<IsFormFileAttribute>() != null
            )
            .Select(a => new SharedReference(a))
            .First();

        ToFormFileAsyncExtention = allTypes
            .Where(t =>
                t.IsClass &&
                t.GetCustomAttribute<IsToFormFileAsyncExtentionAttribute>() != null
            )
            .Select(a => new SharedReference(a))
            .First();

        BaseResponse = new SharedReference("gAPI.Dtos", "BaseResponse");
        BaseResponseT = new SharedReference("gAPI.Dtos", "BaseResponseT");
        BaseListResponseT = new SharedReference("gAPI.Dtos", "BaseListResponseT");

        LoaderView = allTypes
            .Where(a => a.Name == "LoaderView")
            .Select(a => new SharedReference(a))
            .FirstOrDefault() ?? throw new Exception("cannot find EditForm");

        ErrorView = allTypes
            .Where(a => a.Name == "ErrorView")
            .Select(a => new SharedReference(a))
            .FirstOrDefault() ?? throw new Exception("cannot find EditForm");

        RedirectToHome = allTypes
            .Where(a => a.Name == "RedirectToHome")
            .Select(a => new SharedReference(a))
            .FirstOrDefault() ?? throw new Exception("cannot find RedirectToHome");

        RedirectToLogin = allTypes
            .Where(a => a.Name == "RedirectToLogin")
            .Select(a => new SharedReference(a))
            .FirstOrDefault() ?? throw new Exception("cannot find RedirectToLogin");
    }

    public FrontendConfig Config { get; }
    public Interface[] Interfaces { get; }
    public EnumDto[] Enums { get; }
    public Dto[] Dtos { get; }
    public IEnumerable<Client?> Clients
        => Interfaces.Select(a => a.Client);
    public SharedReference IClientAuthenticationService { get; }
    public SharedReference ClientAuthenticationService { get; }
    public SharedReference ListDataSource { get; }
    public SharedReference ItemDataSource { get; }
    public SharedReference FormFile { get; }
    public SharedReference ToFormFileAsyncExtention { get; }
    public SharedReference BaseResponse { get; }
    public SharedReference BaseResponseT { get; }
    public SharedReference BaseListResponseT { get; }
    public SharedReference LoaderView { get; }
    public SharedReference ErrorView { get; }
    public SharedReference RedirectToHome { get; }
    public SharedReference RedirectToLogin { get; }
}
