using gAPI.CodeGen.Backend.Models.Config;

namespace gAPI.CodeGen.Backend.Models;

public class SharedReferences
{
    public SharedReferences(BackendConfig config)
    {
        BaseResponse = new SharedReference("gAPI.Dtos.BaseResponse");
        BaseResponseT = new SharedReference("gAPI.Dtos.BaseResponseT");
        BaseListResponseT = new SharedReference("gAPI.Dtos.BaseListResponseT");
        CreateServerConfigExtension = new SharedReference("gAPI.Extensions.CreateServerConfigExtension");
        AddStorageExtension = new SharedReference("gAPI.Storage.AddStorageExtension");
        ServerConfig = new SharedReference("gAPI.Dtos.ServerConfig");

        AddAutoApiExtension = new SharedReference(config.Api_Namespace, "AddAutoApiExtension"); // todo??
        AddAutoSseExtension = new SharedReference(config.Api_Namespace, "AddAutoSseExtension"); // todo??

        IUseCase = new SharedReference("gAPI.Interfaces.IUseCase");
        Mapping = new SharedReference("gAPI.Interfaces.Mapping");

        GapiIServerAuthenticationService = new SharedReference("gAPI.Interfaces.IServerAuthenticationService");
        AuthenticationHeaders = new SharedReference("gAPI.Authentication.AuthenticationHeaders");
        AuthenticationInitializeResult = new SharedReference("gAPI.Authentication.AuthenticationInitializeResult");

        IsHidden = new SharedReference("gAPI.Attributes.IsHidden");
        IsStateDto = new SharedReference("gAPI.Attributes.IsStateDto");

        IStorageService = new SharedReference("gAPI.Storage.IStorageService");
        ApplyOrderByExtension = new SharedReference("gAPI.Extensions.ApplyOrderBy");

        StringExtensions = new SharedReference("gAPI.Extensions.StringExtensions");
        IsAuthorized = new SharedReference("gAPI.Attributes.IsAuthorized");
        IsNotAuthorized = new SharedReference("gAPI.Attributes.IsNotAuthorized");
        GenerateApi = new SharedReference("gAPI.Attributes.GenerateApi");
        IsPage = new SharedReference("gAPI.Attributes.IsPage");
        IsStorageFileUrlProperty = new SharedReference("gAPI.Attributes.IsStorageFileUrlProperty");
        IStorageFileDto = new SharedReference("gAPI.Storage.IStorageFileDto");
        IStorageFile = new SharedReference("gAPI.Storage.IStorageFile");
        IsPassword = new SharedReference("gAPI.Attibutes.IsPassword");
        IsLogoffPage = new SharedReference("gAPI.Attibutes.IsLogoffPage");
    }

    public SharedReference BaseResponse { get; }
    public SharedReference BaseResponseT { get; }
    public SharedReference BaseListResponseT { get; }
    public SharedReference CreateServerConfigExtension { get; }
    public SharedReference AddAutoApiExtension { get; }
    public SharedReference AddAutoSseExtension { get; }
    public SharedReference AddStorageExtension { get; }
    public SharedReference ServerConfig { get; }
    public SharedReference IUseCase { get; }
    public SharedReference Mapping { get; }
    public SharedReference GapiIServerAuthenticationService { get; }
    public SharedReference AuthenticationHeaders { get; }
    public SharedReference AuthenticationInitializeResult { get; }
    public SharedReference IStorageService { get; }
    public SharedReference IsStateDto { get; }
    public SharedReference ApplyOrderByExtension { get; }
    public SharedReference StringExtensions { get; }
    public SharedReference IsAuthorized { get; }
    public SharedReference IsNotAuthorized { get; }

    public SharedReference IsHidden { get; }
    public SharedReference GenerateApi { get; }
    public SharedReference IsPage { get; }
    public SharedReference IsStorageFileUrlProperty { get; }
    public SharedReference IStorageFileDto { get; }
    public SharedReference IStorageFile { get; }
    public SharedReference IsPassword { get; }
    public SharedReference IsLogoffPage { get; }
}