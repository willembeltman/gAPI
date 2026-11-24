namespace gAPI.Core.Server.Enums;

public enum FabricHostToClientMessageEnum
{
    GetSessionCookieDataResponse,
    SynchronizeFabricIds,

    SubscribeAck,
    UnsubscribeAck,

    FabricSendAndForgetRequest,

    FabricSendRequest,
    FabricSendRequestCancelled,
    SendRequestDone,

    FabricInvokeRequest,
    FabricInvokeRequestCancelled,
    InvokeRequestAck,
    InvokeRequestDone,

    StreamingRequest,
    StreamingResponse,
}