namespace gAPI.Core.Server.Enums;

public enum FabricClientToHostMessageEnum
{
    Subscribe,
    Unsubscribe,

    SendAndForgetRequest,
    SendAndForgetRequestCancelled,

    SendRequest,
    SendRequestCancelled,
    SendRequestDone,

    InvokeRequest,
    InvokeRequestCancelled,
    InvokeRequestAck,
    InvokeRequestDone,

    StreamingRequest_ResponseEnumerable,
    StreamingResponse_ArgumentEnumerable,

    StreamingRequest_ArgumentEnumerable,
    StreamingResponse_ResponseEnumerable,


    UpdateSession,
    ClearSession,
    GetSessionCookieData,

    BusSubscribe,
    BusUnsubscribe,

    BusFireAndForget,
    BusFireAndForgetCancelled,

    BusSendRequest,
    BusSendRequestCancelled,
    BusSendRequestDone,

    BusInvokeRequest,
    BusInvokeRequestCancelled,
    BusInvokeRequestAck,
    BusInvokeRequestDone,

    BusStreamingRequest_ResponseEnumerable,
    BusStreamingResponse_ResponseEnumerable,
    BusStreamingRequest_ArgumentEnumerable,
    BusStreamingResponse_ArgumentEnumerable,
}