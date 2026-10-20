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
    InvokeRequestDone,

    StreamingRequestServerToClient,
    StreamingResponseServerToClient,

    StreamingRequestClientToServer,
    StreamingResponseClientToServer,


    UpdateSession,
    ClearSession,
    GetSessionCookieData,

    BusSubscribe,
    BusUnsubscribe,

    BusFireAndForget,
    BusFireAndForgetCancelled,

    BusSendRequest,
    BusSendRequestCancelled,
    BusSendRequestAck,

    BusInvokeRequest,
    BusInvokeRequestCancelled,
    BusInvokeRequestAck,

    BusStreamingRequest_ResponseEnumerable,
    BusStreamingResponse_ResponseEnumerable,

    BusStreamingRequest_ArgumentEnumerable,
    BusStreamingResponse_ArgumentEnumerable,
}