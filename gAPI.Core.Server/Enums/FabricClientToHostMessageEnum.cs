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
    InvokeRequestDone,

    UpdateSession,
    ClearSession,
    GetSessionCookieData
}