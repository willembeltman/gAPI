using gAPI.AutoApiClient.Models.Configs;
using Microsoft.CodeAnalysis;
using System;

namespace gAPI.AutoApiClient.Models;

public class SharedReferences
{
    public SharedReferences(Compilation compilation, ClientConfig config, INamedTypeSymbol[] allSymbols)
    {
        var StateChangedHandler_Symbol =
            compilation.GetTypeByMetadataName("gAPI.Delegates.StateChangedHandler") ??
            throw new Exception("gAPI.Delegates.StateChangedHandler was not found. " +
                "Please reference the gAPI package on the same project as gAPI.AutoComponents references.");
        StateChangedHandler = new SharedReference(StateChangedHandler_Symbol);

        var baseResponse_Symbol =
            compilation.GetTypeByMetadataName("gAPI.Dtos.BaseResponse") ??
            throw new Exception("gAPI.Dtos.BaseResponse was not found. " +
                "Please reference the gAPI package on the same project as gAPI.AutoComponents references.");
        BaseResponse = new SharedReference(baseResponse_Symbol);
        var baseResponseT_Symbol =
            compilation.GetTypeByMetadataName("gAPI.Dtos.BaseResponseT`1") ??
            throw new Exception("gAPI.Dtos.BaseResponseT was not found. " +
                "Please reference the gAPI package on the same project as gAPI.AutoComponents references.");
        BaseResponseT = new SharedReference(baseResponseT_Symbol);
        var baseListResponseT_Symbol =
            compilation.GetTypeByMetadataName("gAPI.Dtos.BaseListResponseT`1") ??
            throw new Exception("gAPI.Dtos.BaseListResponseT was not found. " +
                "Please reference the gAPI package on the same project as gAPI.AutoComponents references.");
        BaseListResponseT = new SharedReference(baseListResponseT_Symbol);
    }

    public SharedReference StateChangedHandler { get; }
    public SharedReference BaseResponse { get; }
    public SharedReference BaseResponseT { get; }
    public SharedReference BaseListResponseT { get; }
}