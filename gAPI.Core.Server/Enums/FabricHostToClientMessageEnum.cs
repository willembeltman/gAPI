namespace gAPI.Core.Server.Enums;

public enum FabricHostToClientMessageEnum
{
    SendRequest,
    SendRequestDone,
    SendRequestException,
    InvokeArgumentRequest,
    InvokeArgumentResponse,
    InvokeRequest,
    InvokeResponse,
    InvokeResponseDone,
    InvokeResponseException,

    GetSessionCookieDataResponse,
}