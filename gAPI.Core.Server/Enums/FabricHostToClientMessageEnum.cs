namespace gAPI.Core.Server.Enums;

public enum FabricHostToClientMessageEnum
{
    GetSessionCookieDataResponse,
    SynchronizeFabricIds,
    Log,


    FabricSendAndForgetRequest,
    FabricSendAndForgetRequestCancelled,

    FabricSendRequest,
    FabricSendRequestCancelled,
    SendRequestDone,

    FabricInvokeRequest,
    FabricInvokeRequestCancelled,
    InvokeRequestAck,
    InvokeRequestDone,

    StreamingRequest_ResponseEnumerable,
    StreamingResponse_ArgumentEnumerable,
    StreamingRequest_ArgumentEnumerable,
    StreamingResponse_ResponseEnumerable,


    BusFireAndForget,
    BusFireAndForgetCancelled,

    BusSendRequest,
    BusSendRequestCancelled,
    BusSendRequestDone,

    BusInvokeRequest,
    BusInvokeRequestCancelled,
    BusInvokeRequestAck,
    BusInvokeRequestDone,

    BusStreamingRequest,
    BusStreamingResponse
}