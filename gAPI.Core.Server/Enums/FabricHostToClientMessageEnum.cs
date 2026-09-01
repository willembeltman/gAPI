namespace gAPI.Core.Server.Enums;

public enum FabricHostToClientMessageEnum
{
    SendRequest,
    SendRequestDone,
    InvokeArgumentRequest,
    InvokeArgumentResponse,
    InvokeRequest,
    InvokeResponse,
    InvokeResponseDone,

    GetSessionCookieDataResponse,
}