namespace gAPI.Core.Server.Enums;

public enum PlaneFabricHostToClientMessageEnum
{
    SynchronizeFabricIds,

    SubscribeAck,
    UnsubscribeAck,

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