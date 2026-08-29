namespace gAPI.Core.Server.Enums;

public enum FabricHostToClientMessageEnum
{
    SendRequest,
    SendArgumentedRequest,
    InvokeArgumentRequest,
    InvokeArgumentResponse,
    InvokeRequest,
    InvokeResponse,
    InvokeResponseDone,
    GetSessionCookieDataResponse,
}