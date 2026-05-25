using gAPI.CodeGen.Backend.Models.Config;

namespace gAPI.CodeGen.Backend.Models;

public class SharedReferences()
{
    public SharedReference BaseResponse { get; } = new SharedReference("gAPI.Core.Dtos.BaseResponse");
    public SharedReference BaseResponseT { get; } = new SharedReference("gAPI.Core.Dtos.BaseResponseT");
    public SharedReference BaseListResponseT { get; } = new SharedReference("gAPI.Core.Dtos.BaseListResponseT");
    public SharedReference CreateServerConfigExtension { get; } = new SharedReference("gAPI.Core.Extensions.CreateServerConfigExtension");
    public SharedReference IUseCase { get; } = new SharedReference("gAPI.Core.Interfaces.IUseCase");
    public SharedReference Mapping { get; } = new SharedReference("gAPI.Core.Interfaces.Mapping");
    public SharedReference IStorageService { get; } = new SharedReference("gAPI.Core.Server.Storage.IStorageService");
    public SharedReference IsStateDto { get; } = new SharedReference("gAPI.Core.Attributes.IsStateDto");
    public SharedReference ApplyOrderByExtension { get; } = new SharedReference("gAPI.Core.Server.Extensions.ApplyOrderBy");
    public SharedReference IsStorageFileUrlProperty { get; } = new SharedReference("gAPI.Core.Attributes.IsStorageFileUrlProperty");
    public SharedReference IStorageFileDto { get; } = new SharedReference("gAPI.Storage.IStorageFileDto");
    public SharedReference IAuthenticationService { get; } = new SharedReference("gAPI.Core.Server.IAuthenticationService");
    public SharedReference BaseResponseErrorEnum { get; } = new SharedReference("gAPI.Core.Enums.BaseResponseErrorEnum");
}