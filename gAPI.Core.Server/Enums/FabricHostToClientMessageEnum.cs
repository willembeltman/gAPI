namespace gAPI.Core.Server.Enums;

public enum FabricHostToClientMessageEnum
{
    SynchronizeFabricIds,
    Log,

    SendRequest,
    SendRequestDone,
    SendRequestCancelled,
    StreamingRequest,
    StreamingResponse,
    StreamingRequestCancelled,
    InvokeRequest,
    InvokeResponse,
    InvokeResponseDone,
    InvokeRequestCancelled,

    GetSessionCookieDataResponse,
}