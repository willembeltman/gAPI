namespace gAPI.Core.Enums;

public enum WssServerToClientMessageEnum
{
    SynchronizeClientIds,

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

    FabricStreamingRequest,
    FabricStreamingResponse
}