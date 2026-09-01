namespace gAPI.Core.Server.Enums;

public enum FabricHostToClientMessageEnum
{
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

    GetSessionCookieDataResponse,
}