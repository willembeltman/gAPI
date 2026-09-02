namespace gAPI.Core.Server.Enums;

public enum FabricClientToHostMessageEnum
{
    Subscribe,
    Unsubscribe,

    SendRequest,
    SendRequestDone,
    SendRequestCancelled,

    StreamingRequest,
    StreamingResponse,

    InvokeRequest,
    InvokeRequestCancelled,
    InvokeResponse,
    InvokeResponseDone,

    UpdateSession,
    ClearSession,
    GetSessionCookieData
}