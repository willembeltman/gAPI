namespace gAPI.Core.Enums;

public enum WssServerToClientMessageEnum
{
    SynchronizeClientIds,

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