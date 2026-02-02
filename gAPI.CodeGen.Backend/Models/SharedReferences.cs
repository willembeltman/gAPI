using gAPI.CodeGen.Backend.Models.Config;

namespace gAPI.CodeGen.Backend.Models;

public class SharedReferences
{
    public SharedReferences(BackendConfig config)
    {
        BaseResponse = new SharedReference("gAPI.Dtos.BaseResponse");
        BaseResponseT = new SharedReference("gAPI.Dtos.BaseResponseT");
        BaseListResponseT = new SharedReference("gAPI.Dtos.BaseListResponseT");
        CreateServerConfigExtention = new SharedReference("gAPI.Extentions.CreateServerConfigExtention");
        AddStorageExtention = new SharedReference("gAPI.Storage.AddStorageExtention");
        ServerConfig = new SharedReference("gAPI.Dtos.ServerConfig");

        AddAutoApiExtention = new SharedReference(config.Api_Namespace, "AddAutoApiExtention"); // todo??
        AddAutoSseExtention = new SharedReference(config.Api_Namespace, "AddAutoSseExtention"); // todo??

        IUseCase = new SharedReference("gAPI.Interfaces.IUseCase");
        Mapping = new SharedReference("gAPI.Interfaces.Mapping");

        GapiIServerAuthenticationService = new SharedReference("gAPI.Interfaces.IServerAuthenticationService");
        AuthenticationHeaders = new SharedReference("gAPI.Authentication.AuthenticationHeaders");
        AuthenticationInitializeResult = new SharedReference("gAPI.Authentication.AuthenticationInitializeResult");

        IsHidden = new SharedReference("gAPI.Attributes.IsHidden");
        IsStateDto = new SharedReference("gAPI.Attributes.IsStateDto");

        IStorageService = new SharedReference("gAPI.Storage.IStorageService");
        ApplyOrderByExtention = new SharedReference("gAPI.Extentions.ApplyOrderBy");

        StringExtentions = new SharedReference("gAPI.Extentions.StringExtentions");
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
    public SharedReference CreateServerConfigExtention { get; }
    public SharedReference AddAutoApiExtention { get; }
    public SharedReference AddAutoSseExtention { get; }
    public SharedReference AddStorageExtention { get; }
    public SharedReference ServerConfig { get; }
    public SharedReference IUseCase { get; }
    public SharedReference Mapping { get; }
    public SharedReference GapiIServerAuthenticationService { get; }
    public SharedReference AuthenticationHeaders { get; }
    public SharedReference AuthenticationInitializeResult { get; }
    public SharedReference IStorageService { get; }
    public SharedReference IsStateDto { get; }
    public SharedReference ApplyOrderByExtention { get; }
    public SharedReference StringExtentions { get; }
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