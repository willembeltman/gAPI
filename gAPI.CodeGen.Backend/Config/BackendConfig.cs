namespace gAPI.CodeGen.Backend.Config;

public class BackendConfig
{
    public BackendConfig(
        Type dbContextType,
        DirectoryInfo sharedDtoDirectory, string sharedDtoNamespace,
        DirectoryInfo sharedResponseDtoDirectory, string sharedResponseDtoNamespace,
        DirectoryInfo coreInterfacesDirectory, string coreInterfacesNamespace,
        DirectoryInfo coreModelsDirectory, string coreModelsNamespace,
        DirectoryInfo coreCrudHandlersDirectory, string coreCrudHandlersNamespace,
        DirectoryInfo coreCrudMappingsDirectory, string coreCrudMappingsNamespace,
        DirectoryInfo sharedInterfacesDirectory, string sharedInterfacesNamespace,
        DirectoryInfo coreCrudServicesDirectory, string coreCrudServicesNamespace)
    {
        DbContextType = dbContextType;
        DtoDirectory = sharedDtoDirectory;
        DtoNamespace = sharedDtoNamespace;
        ResponseDtoDirectory = sharedResponseDtoDirectory;
        ResponseDtoNamespace = sharedResponseDtoNamespace;
        BusinessInterfacesDirectory = coreInterfacesDirectory;
        BusinessInterfacesNamespace = coreInterfacesNamespace;
        BusinessModelsDirectory = coreModelsDirectory;
        BusinessModelsNamespace = coreModelsNamespace;
        CrudHandlersDirectory = coreCrudHandlersDirectory;
        CrudHandlersNamespace = coreCrudHandlersNamespace;
        CustomMappingsDirectory = coreCrudMappingsDirectory;
        CustomMappingsNamespace = coreCrudMappingsNamespace;
        CrudServiceInterfacesDirectory = sharedInterfacesDirectory;
        CrudServiceInterfacesNamespace = sharedInterfacesNamespace;
        CrudServicesDirectory = coreCrudServicesDirectory;
        CrudServicesNamespace = coreCrudServicesNamespace;
    }
    public Type DbContextType { get; }
    public DirectoryInfo DtoDirectory { get; }
    public string DtoNamespace { get; }

    public DirectoryInfo ResponseDtoDirectory { get; }
    public string ResponseDtoNamespace { get; }

    public DirectoryInfo BusinessModelsDirectory { get; }
    public string BusinessModelsNamespace { get; }

    public DirectoryInfo BusinessInterfacesDirectory { get; }
    public string BusinessInterfacesNamespace { get; }

    public DirectoryInfo CrudHandlersDirectory { get; }
    public string CrudHandlersNamespace { get; }

    public DirectoryInfo CustomMappingsDirectory { get; }
    public string CustomMappingsNamespace { get; }

    public DirectoryInfo CrudServiceInterfacesDirectory { get; }
    public string CrudServiceInterfacesNamespace { get; }

    public DirectoryInfo CrudServicesDirectory { get; }
    public string CrudServicesNamespace { get; }
}
