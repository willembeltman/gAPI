namespace gAPI.CodeGen.Backend.Models.Config;

public class BackendConfig
{
    public BackendConfig(
        Type dbContextType,
        DirectoryInfo data_AuthenticationDirectory, string data_AuthenticationNamespace,
        DirectoryInfo shared_DtosDirectory, string shared_DtosNamespace,
        DirectoryInfo shared_InterfacesDirectory, string shared_InterfacesNamespace,
        DirectoryInfo shared_RequestDtosDirectory, string shared_RequestDtosNamespace,
        DirectoryInfo shared_ResponseDtosDirectory, string shared_ResponseDtosNamespace,
        DirectoryInfo shared_StateDtosDirectory, string shared_StateDtosNamespace,
        DirectoryInfo core_AuthenticationDirectory, string core_AuthenticationNamespace,
        DirectoryInfo core_ServicesDirectory, string core_ServicesNamespace,
        DirectoryInfo core_CrudUseCasesDirectory, string core_CrudUseCasesNamespace,
        DirectoryInfo core_CrudMappingsDirectory, string core_CrudMappingsNamespace,
        DirectoryInfo core_CrudServicesDirectory, string core_CrudServicesNamespace,
        DirectoryInfo api_Directory, string api_Namespace)
    {
        DbContextType = dbContextType;
        Data_AuthenticationDirectory = data_AuthenticationDirectory;
        Data_AuthenticationNamespace = data_AuthenticationNamespace;
        Shared_DtosDirectory = shared_DtosDirectory;
        Shared_DtosNamespace = shared_DtosNamespace;
        Shared_StateDtosDirectory = shared_StateDtosDirectory;
        Shared_StateDtosNamespace = shared_StateDtosNamespace;
        Shared_InterfacesDirectory = shared_InterfacesDirectory;
        Shared_InterfacesNamespace = shared_InterfacesNamespace;
        Shared_RequestDtosDirectory = shared_RequestDtosDirectory;
        Shared_RequestDtosNamespace = shared_RequestDtosNamespace;
        Shared_ResponseDtosDirectory = shared_ResponseDtosDirectory;
        Shared_ResponseDtosNamespace = shared_ResponseDtosNamespace;

        Core_AuthenticationDirectory = core_AuthenticationDirectory;
        Core_AuthenticationNamespace = core_AuthenticationNamespace;
        Core_ServicesDirectory = core_ServicesDirectory;
        Core_ServicesNamespace = core_ServicesNamespace;
        Core_CrudUseCasesDirectory = core_CrudUseCasesDirectory;
        Core_CrudUseCasesNamespace = core_CrudUseCasesNamespace;
        Core_CrudMappingsDirectory = core_CrudMappingsDirectory;
        Core_CrudMappingsNamespace = core_CrudMappingsNamespace;
        Core_CrudServicesDirectory = core_CrudServicesDirectory;
        Core_CrudServicesNamespace = core_CrudServicesNamespace;
        Api_Directory = api_Directory;
        Api_Namespace = api_Namespace;


    }
    public Type DbContextType { get; }

    public DirectoryInfo Shared_DtosDirectory { get; }
    public string Shared_DtosNamespace { get; }

    public DirectoryInfo Shared_StateDtosDirectory { get; }
    public string Shared_StateDtosNamespace { get; }

    public DirectoryInfo Api_Directory { get; }
    public string Api_Namespace { get; }

    public DirectoryInfo Data_AuthenticationDirectory { get; }
    public string Data_AuthenticationNamespace { get; }

    public DirectoryInfo Core_CrudUseCasesDirectory { get; }
    public string Core_CrudUseCasesNamespace { get; }

    public DirectoryInfo Core_CrudMappingsDirectory { get; }
    public string Core_CrudMappingsNamespace { get; }

    public DirectoryInfo Shared_InterfacesDirectory { get; }
    public string Shared_InterfacesNamespace { get; }

    public DirectoryInfo Core_CrudServicesDirectory { get; }
    public string Core_CrudServicesNamespace { get; }

    public DirectoryInfo Core_AuthenticationDirectory { get; }
    public string Core_AuthenticationNamespace { get; }

    public DirectoryInfo Core_ServicesDirectory { get; }
    public string Core_ServicesNamespace { get; }

    public DirectoryInfo? Shared_RequestDtosDirectory { get; }
    public string? Shared_RequestDtosNamespace { get; }
    public DirectoryInfo Shared_ResponseDtosDirectory { get; }
    public string Shared_ResponseDtosNamespace { get; }
}
