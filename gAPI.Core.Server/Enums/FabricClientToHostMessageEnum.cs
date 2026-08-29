namespace gAPI.Core.Server.Enums;

public enum FabricClientToHostMessageEnum
{
    Subscribe,
    Unsubscribe,
    SendRequest,
    SendArgumentedRequest,
    InvokeArgumentRequest,
    InvokeArgumentResponse,
    InvokeRequest,
    InvokeResponse,
    InvokeResponseDone,
    UpdateSession,
    ClearSession,
    GetSessionCookieData
}