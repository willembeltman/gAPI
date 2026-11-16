namespace gAPI.CodeGen.Backend.Models.Config;

public record BackendConfig(
    Type DbContextType,

    DirectoryInfo Shared_DtosDirectory,
    string Shared_DtosNamespace,
    string Shared_DtosEnd,

    DirectoryInfo Shared_CrudInterfacesDirectory,
    string Shared_CrudInterfacesNamespace,
    string Shared_CrudInterfacesEnd,

    DirectoryInfo Shared_StateDtosDirectory,
    string Shared_StateDtosNamespace,
    string Shared_StateDtoName,

    DirectoryInfo Core_CrudUseCasesDirectory,
    string Core_CrudUseCasesNamespace,
    string Core_CrudUseCasesEnd,

    DirectoryInfo Core_CrudMappingsDirectory,
    string Core_CrudMappingsNamespace,
    string Core_CrudMappingsEnd,

    DirectoryInfo Core_StateMappingDirectory,
    string Core_StateMappingNamespace,
    string Core_StateMappingBegin,
    string Core_StateMappingEnd,
    string Core_StateMappingName,

    DirectoryInfo Core_CrudServicesDirectory,
    string Core_CrudServicesNamespace,
    string Core_CrudServicesEnd,

    DirectoryInfo Extensions_Directory,
    string Extensions_Namespace,

    bool OverwriteMappers = true,
    bool OverwriteStateMapping = false,
    bool OverwriteServices = true,
    bool OverwriteServiceInterfaces = true,
    bool OverwriteUseCases = false);
