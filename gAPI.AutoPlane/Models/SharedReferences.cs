using gAPI.AutoPlane.Helpers;
using Microsoft.CodeAnalysis;

namespace gAPI.AutoPlane.Models;

public class SharedReferences
{
    public SharedReferences(ServiceContext serviceContext, INamedTypeSymbol[] allSymbols)
    {
        PlaneConnectionCollection = SharedReferenceFinder.Find("gAPI.Core.Server.Collections.PlaneConnectionCollection", allSymbols);

        ServiceId = SharedReferenceFinder.Find("gAPI.Core.Ids.ServiceId", allSymbols);
        ServiceMethodId = SharedReferenceFinder.Find("gAPI.Core.Ids.ServiceMethodId", allSymbols);
        RequestId = SharedReferenceFinder.Find("gAPI.Core.Ids.RequestId", allSymbols);

        BackPlaneConfig = SharedReferenceFinder.Find("gAPI.Core.Server.Config.BackPlaneConfig", allSymbols);
        PlaneRoutingDto = SharedReferenceFinder.Find("gAPI.Core.Server.Dtos.PlaneRoutingDto", allSymbols);
        PlaneSendRequestDto = SharedReferenceFinder.Find("gAPI.Core.Server.Dtos.PlaneSendRequestDto", allSymbols);
        PlaneInvokeRequestDto = SharedReferenceFinder.Find("gAPI.Core.Server.Dtos.PlaneInvokeRequestDto", allSymbols);
        ServiceSubscriptionKeyDto = SharedReferenceFinder.Find("gAPI.Core.Server.Dtos.ServiceSubscriptionKeyDto", allSymbols);
        StreamingResponseDto = SharedReferenceFinder.Find("gAPI.Core.Dtos.StreamingResponseDto", allSymbols);

        IBasePlaneConnection = SharedReferenceFinder.Find("gAPI.Core.Server.Interfaces.IBasePlaneConnection", allSymbols);
        CancellationTokenSourceDispose = SharedReferenceFinder.Find("gAPI.Core.Helpers.CancellationTokenSourceDispose", allSymbols);

        PlaneToChoice = SharedReferenceFinder.Find("gAPI.Core.Enums.PlaneToChoice", allSymbols);
        PlaneConnectionId = SharedReferenceFinder.Find("gAPI.Core.Ids.PlaneConnectionId", allSymbols);
        ServiceSubscriptionId = SharedReferenceFinder.Find("gAPI.Core.Ids.ServiceSubscriptionId", allSymbols);
        IHostedPlaneService = SharedReferenceFinder.Find("gAPI.Core.Server.Interfaces.IHostedPlaneService", allSymbols);
        IPlaneServiceSubscription = SharedReferenceFinder.Find("gAPI.Core.Server.Interfaces.IPlaneServiceSubscription", allSymbols);

        BaseFabricConnection = SharedReferenceFinder.TryFind("gAPI.Core.BackPlane.Fabric.FabricBasePlaneConnection", allSymbols);
        RabbitMQBasePlaneConnection = SharedReferenceFinder.TryFind("gAPI.Core.BackPlane.RabbitMQ.RabbitMQBasePlaneConnection", allSymbols);
        AzureBasePlaneConnection = SharedReferenceFinder.TryFind("gAPI.Core.BackPlane.AzureServicePlane.AzureBasePlaneConnection", allSymbols);

        FabricClient = SharedReferenceFinder.TryFind("gAPI.Core.Server.Fabric.PlaneFabricClient", allSymbols);
        SessionCache = SharedReferenceFinder.TryFind("gAPI.Core.Server.Collections.SessionCache", allSymbols);
        StreamingCache = SharedReferenceFinder.TryFind("gAPI.Core.Server.Collections.StreamingCache", allSymbols);
        ServiceSubscriptionCollection = SharedReferenceFinder.TryFind("gAPI.Core.Server.Collections.ServiceSubscriptionCollection", allSymbols);
        DeliveryMode = SharedReferenceFinder.Find("gAPI.Core.Enums.DeliveryMode", allSymbols);
    }

    public SharedReference PlaneConnectionCollection { get; }
    public SharedReference ServiceId { get; }
    public SharedReference ServiceMethodId { get; }
    public SharedReference RequestId { get; }
    public SharedReference BackPlaneConfig { get; }
    public SharedReference StreamingResponseDto { get; }
    public SharedReference PlaneSendRequestDto { get; }
    public SharedReference PlaneInvokeRequestDto { get; }
    public SharedReference ServiceSubscriptionKeyDto { get; }
    public SharedReference PlaneRoutingDto { get; }
    public SharedReference IBasePlaneConnection { get; }
    public SharedReference CancellationTokenSourceDispose { get; }
    public SharedReference PlaneToChoice { get; }
    public SharedReference PlaneConnectionId { get; }
    public SharedReference ServiceSubscriptionId { get; }
    public SharedReference IHostedPlaneService { get; }
    public SharedReference IPlaneServiceSubscription { get; }
    public SharedReference? BaseFabricConnection { get; }
    public SharedReference? RabbitMQBasePlaneConnection { get; }
    public SharedReference? AzureBasePlaneConnection { get; }
    public SharedReference? FabricClient { get; }
    public SharedReference? SessionCache { get; }
    public SharedReference? StreamingCache { get; }
    public SharedReference? ServiceSubscriptionCollection { get; }
    public SharedReference DeliveryMode { get; }
}