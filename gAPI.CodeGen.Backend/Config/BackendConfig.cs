namespace gAPI.CodeGen.Backend.Config;

public class BackendConfig
{
    public BackendConfig(
        Type dbContextType,
        DirectoryInfo dtoDirectory, string dtoNamespace,
        DirectoryInfo requestDtoDirectory, string requestDtoNamespace,
        DirectoryInfo responseDtoDirectory, string responseDtoNamespace,
        DirectoryInfo businessInterfacesDirectory, string businessInterfacesNamespace,
        DirectoryInfo businessExtentionsDirectory, string businessExtentionsNamespace,
        DirectoryInfo businessModelsDirectory, string businessModelsNamespace,
        DirectoryInfo businessServicesDirectory, string businessServicesNamespace,
        DirectoryInfo crudHandlerInterfacesDirectory, string crudHandlerInterfacesNamespace,
        DirectoryInfo crudHandlersDirectory, string crudHandlersNamespace,
        DirectoryInfo crudMappingsDirectory, string crudMappingsNamespace,
        DirectoryInfo sharedInterfacesDirectory, string sharedInterfacesNamespace,
        DirectoryInfo crudServicesDirectory, string crudServicesNamespace)
    {
        DbContextType = dbContextType;
        DtoDirectory = dtoDirectory;
        DtoNamespace = dtoNamespace;
        RequestDtoDirectory = requestDtoDirectory;
        RequestDtoNamespace = requestDtoNamespace;
        ResponseDtoDirectory = responseDtoDirectory;
        ResponseDtoNamespace = responseDtoNamespace;
        BusinessInterfacesDirectory = businessInterfacesDirectory;
        BusinessInterfacesNamespace = businessInterfacesNamespace;
        BusinessModelsDirectory = businessModelsDirectory;
        BusinessModelsNamespace = businessModelsNamespace;
        BusinessServicesDirectory = businessServicesDirectory;
        BusinessServicesNamespace = businessServicesNamespace;
        BusinessExtentionsDirectory = businessExtentionsDirectory;
        BusinessExtentionsNamespace = businessExtentionsNamespace;
        CrudHandlerInterfacesDirectory = crudHandlerInterfacesDirectory;
        CrudHandlerInterfacesNamespace = crudHandlerInterfacesNamespace;
        CrudHandlersDirectory = crudHandlersDirectory;
        CrudHandlersNamespace = crudHandlersNamespace;
        CustomMappingsDirectory = crudMappingsDirectory;
        CustomMappingsNamespace = crudMappingsNamespace;
        CrudServiceInterfacesDirectory = sharedInterfacesDirectory;
        CrudServiceInterfacesNamespace = sharedInterfacesNamespace;
        CrudServicesDirectory = crudServicesDirectory;
        CrudServicesNamespace = crudServicesNamespace;
    }
    public Type DbContextType { get; }
    public DirectoryInfo DtoDirectory { get; }
    public string DtoNamespace { get; }

    public DirectoryInfo RequestDtoDirectory { get; }
    public string RequestDtoNamespace { get; }

    public DirectoryInfo ResponseDtoDirectory { get; }
    public string ResponseDtoNamespace { get; }

    public DirectoryInfo BusinessModelsDirectory { get; }
    public string BusinessModelsNamespace { get; }
    public DirectoryInfo BusinessServicesDirectory { get; }
    public string BusinessServicesNamespace { get; }
    public DirectoryInfo BusinessExtentionsDirectory { get; }
    public string BusinessExtentionsNamespace { get; }

    public DirectoryInfo BusinessInterfacesDirectory { get; }
    public string BusinessInterfacesNamespace { get; }

    public DirectoryInfo CrudHandlerInterfacesDirectory { get; internal set; }
    public string CrudHandlerInterfacesNamespace { get; internal set; }

    public DirectoryInfo CrudHandlersDirectory { get; }
    public string CrudHandlersNamespace { get; }

    public DirectoryInfo CustomMappingsDirectory { get; }
    public string CustomMappingsNamespace { get; }

    public DirectoryInfo CrudServiceInterfacesDirectory { get; }
    public string CrudServiceInterfacesNamespace { get; }

    public DirectoryInfo CrudServicesDirectory { get; }
    public string CrudServicesNamespace { get; }
}
