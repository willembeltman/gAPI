namespace gAPI.CodeGen.Backend.Models.Config;

public record BackendConfig(
    Type DbContextType,

    DirectoryInfo Shared_DtosDirectory,
    string Shared_DtosNamespace,

    DirectoryInfo Shared_CrudInterfacesDirectory,
    string Shared_CrudInterfacesNamespace,

    DirectoryInfo Shared_StateDtosDirectory,
    string Shared_StateDtosNamespace,

    DirectoryInfo Core_CrudUseCasesDirectory,
    string Core_CrudUseCasesNamespace,

    DirectoryInfo Core_CrudMappingsDirectory,
    string Core_CrudMappingsNamespace,

    DirectoryInfo Core_CrudServicesDirectory,
    string Core_CrudServicesNamespace,

    DirectoryInfo Extensions_Directory,
    string Extensions_Namespace,

    bool OverwriteMappers = true,
    bool OverwriteServices = false, 
    bool OverwriteServiceInterfaces = false, 
    bool OverwriteUseCases = false
    );
