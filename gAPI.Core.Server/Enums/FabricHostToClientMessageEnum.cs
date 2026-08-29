namespace gAPI.Core.Server.Enums;

public enum FabricHostToClientMessageEnum
{
    SendRequest,
    InvokeArgumentRequest,
    InvokeArgumentResponse,
    InvokeRequest,
    InvokeResponse,
    InvokeResponseDone,
    GetSessionCookieDataResponse,
}