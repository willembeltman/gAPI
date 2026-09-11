namespace gAPI.Core.Server.Enums;

public enum FabricHostToClientMessageEnum
{
    FabricSendRequest,
    FabricSendRequestCancelled,
    SendRequestDone,

    FabricInvokeRequest,
    FabricInvokeRequestCancelled,
    InvokeRequestDone,

    StreamingRequestServerToClient,
    StreamingResponseServerToClient,
    StreamingRequestClientToServer,
    StreamingResponseClientToServer,

    GetSessionCookieDataResponse,
    SynchronizeFabricIds,
    Log,
}