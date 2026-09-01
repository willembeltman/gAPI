namespace gAPI.Core.Server.Enums;

public enum FabricClientToHostMessageEnum
{
    Subscribe,
    Unsubscribe,

    SendRequest,
    SendRequestDone,
    SendRequestCancelled,
    InvokeArgumentRequest,
    InvokeArgumentResponse,
    InvokeArgumentCancelled,
    InvokeRequest,
    InvokeRequestCancelled,
    InvokeResponse,
    InvokeResponseDone,

    UpdateSession,
    ClearSession,
    GetSessionCookieData
}