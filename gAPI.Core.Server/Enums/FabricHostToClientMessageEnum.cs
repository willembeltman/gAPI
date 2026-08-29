namespace gAPI.Core.Server.Enums;

public enum FabricHostToClientMessageEnum
{
    SendRequest,
    SendArgumentedRequest,
    SendArgumentedRequestDone,
    InvokeArgumentRequest,
    InvokeArgumentResponse,
    InvokeRequest,
    InvokeResponse,
    InvokeResponseDone,
    GetSessionCookieDataResponse,
}