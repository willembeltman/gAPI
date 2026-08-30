namespace gAPI.Core.Server.Enums;

public enum FabricClientToHostMessageEnum
{
    Subscribe,
    Unsubscribe,

    SendRequest,
    SendRequestDone,
    SendRequestException,
    InvokeArgumentRequest,
    InvokeArgumentResponse,
    InvokeRequest,
    InvokeResponse,
    InvokeResponseDone,
    InvokeResponseException,

    UpdateSession,
    ClearSession,
    GetSessionCookieData
}