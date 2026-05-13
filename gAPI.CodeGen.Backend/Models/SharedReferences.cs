using gAPI.CodeGen.Backend.Models.Config;

namespace gAPI.CodeGen.Backend.Models;

public class SharedReferences(BackendConfig config)
{
    public SharedReference BaseResponse { get; } = new SharedReference("gAPI.Core.Dtos.BaseResponse");
    public SharedReference BaseResponseT { get; } = new SharedReference("gAPI.Core.Dtos.BaseResponseT");
    public SharedReference BaseListResponseT { get; } = new SharedReference("gAPI.Core.Dtos.BaseListResponseT");
    public SharedReference CreateServerConfigExtension { get; } = new SharedReference("gAPI.Core.Extensions.CreateServerConfigExtension");
    public SharedReference AddAutoApiExtension { get; } = new SharedReference(config.Extensions_Namespace, "AddAutoApiExtension"); // todo??
    public SharedReference AddAutoSseExtension { get; } = new SharedReference(config.Extensions_Namespace, "AddAutoSseExtension"); // todo??
    public SharedReference AddStorageExtension { get; } = new SharedReference("gAPI.Storage.AddStorageExtension");
    public SharedReference ServerConfig { get; } = new SharedReference("gAPI.Core.Dtos.ServerConfig");
    public SharedReference IUseCase { get; } = new SharedReference("gAPI.Core.Interfaces.IUseCase");
    public SharedReference Mapping { get; } = new SharedReference("gAPI.Core.Interfaces.Mapping");
    public SharedReference GapiIServerAuthenticationService { get; } = new SharedReference("gAPI.Core.Interfaces.IServerAuthenticationService");
    public SharedReference AuthenticationHeaders { get; } = new SharedReference("gAPI.Core.Server.Authentication.AuthenticationHeaders");
    public SharedReference AuthenticationInitializeResult { get; } = new SharedReference("gAPI.Core.Server.Authentication.AuthenticationInitializeResult");
    public SharedReference IStorageService { get; } = new SharedReference("gAPI.Core.Server.Storage.IStorageService");
    public SharedReference IsStateDto { get; } = new SharedReference("gAPI.Core.Attributes.IsStateDto");
    public SharedReference ApplyOrderByExtension { get; } = new SharedReference("gAPI.Core.Server.Extensions.ApplyOrderBy");
    public SharedReference StringExtensions { get; } = new SharedReference("gAPI.Core.Extensions.StringExtensions");
    public SharedReference IsAuthorized { get; } = new SharedReference("gAPI.Core.Attributes.IsAuthorized");
    public SharedReference IsNotAuthorized { get; } = new SharedReference("gAPI.Core.Attributes.IsNotAuthorized");

    public SharedReference IsHidden { get; } = new SharedReference("gAPI.Core.Attributes.IsHidden");
    public SharedReference GenerateApi { get; } = new SharedReference("gAPI.Core.Attributes.GenerateApi");
    public SharedReference IsPage { get; } = new SharedReference("gAPI.Core.Attributes.IsPage");
    public SharedReference IsStorageFileUrlProperty { get; } = new SharedReference("gAPI.Core.Attributes.IsStorageFileUrlProperty");
    public SharedReference IStorageFileDto { get; } = new SharedReference("gAPI.Storage.IStorageFileDto");
    public SharedReference IStorageFile { get; } = new SharedReference("gAPI.Storage.IStorageFile");
    public SharedReference IsPassword { get; } = new SharedReference("gAPI.Attibutes.IsPassword");
    public SharedReference IsLogoffPage { get; } = new SharedReference("gAPI.Attibutes.IsLogoffPage");
    public SharedReference IServerAuthenticationService { get; } = new SharedReference("gAPI.Core.Interfaces.IServerAuthenticationService");
    public SharedReference IAuthenticationService { get; } = new SharedReference("gAPI.Core.Server.IAuthenticationService");
    public SharedReference BaseResponseErrorEnum { get; } = new SharedReference("gAPI.Core.Enums.BaseResponseErrorEnum");
}