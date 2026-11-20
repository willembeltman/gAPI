namespace gAPI.Core.Server.Enums;

public enum PlaneFabricClientToHostMessageEnum
{
    PlaneSubscribe,
    PlaneUnsubscribe,

    PlaneFireAndForget,

    PlaneSendRequest,
    PlaneSendRequestCancelled,
    PlaneSendRequestDone,

    PlaneInvokeRequest,
    PlaneInvokeRequestCancelled,
    PlaneInvokeRequestAck,
    PlaneInvokeRequestDone,

    PlaneStreamingRequest,
    PlaneStreamingResponse,
}