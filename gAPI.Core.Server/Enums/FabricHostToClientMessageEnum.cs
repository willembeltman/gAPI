namespace gAPI.Core.Server.Enums;

public enum FabricHostToClientMessageEnum
{
    GetSessionCookieDataResponse,
    SynchronizeFabricIds,
    Log,

    SubscribeAck,
    UnsubscribeAck,

    FabricSendAndForgetRequest,
    FabricSendAndForgetRequestCancelled,

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