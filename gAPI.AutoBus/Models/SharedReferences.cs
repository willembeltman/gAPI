using gAPI.AutoBus.Helpers;
using Microsoft.CodeAnalysis;

namespace gAPI.AutoBus.Models;

public class SharedReferences
{
    public SharedReferences(ServiceContext serviceContext, INamedTypeSymbol[] allSymbols)
    {
        BusConnectionCollection = SharedReferenceFinder.Find("gAPI.Core.Server.Collections.BusConnectionCollection", allSymbols);

        ServiceId = SharedReferenceFinder.Find("gAPI.Core.Ids.ServiceId", allSymbols);
        ServiceMethodId = SharedReferenceFinder.Find("gAPI.Core.Ids.ServiceMethodId", allSymbols);
        RequestId = SharedReferenceFinder.Find("gAPI.Core.Ids.RequestId", allSymbols);

        ServiceBusConfig = SharedReferenceFinder.Find("gAPI.Core.Server.Config.ServiceBusConfig", allSymbols);
        BusRoutingDto = SharedReferenceFinder.Find("gAPI.Core.Server.Dtos.BusRoutingDto", allSymbols);
        BusSendRequestDto = SharedReferenceFinder.Find("gAPI.Core.Server.Dtos.BusSendRequestDto", allSymbols);
        BusInvokeRequestDto = SharedReferenceFinder.Find("gAPI.Core.Server.Dtos.BusInvokeRequestDto", allSymbols);
        StreamingResponseDto = SharedReferenceFinder.Find("gAPI.Core.Dtos.StreamingResponseDto", allSymbols);

        IBaseBusConnection = SharedReferenceFinder.Find("gAPI.Core.Server.Interfaces.IBaseBusConnection", allSymbols);
        CancellationTokenSourceDispose = SharedReferenceFinder.Find("gAPI.Core.Helpers.CancellationTokenSourceDispose", allSymbols);

        BusToChoice = SharedReferenceFinder.Find("gAPI.Core.Enums.BusToChoice", allSymbols);
        BusConnectionId = SharedReferenceFinder.Find("gAPI.Core.Ids.BusConnectionId", allSymbols);
        IHostedBusService = SharedReferenceFinder.Find("gAPI.Core.Server.Interfaces.IHostedBusService", allSymbols);
        IBusServiceSubscription = SharedReferenceFinder.Find("gAPI.Core.Server.Interfaces.IBusServiceSubscription", allSymbols);

        BaseFabricConnection = SharedReferenceFinder.TryFind("gAPI.Core.ServiceBus.Fabric.FabricBaseBusConnection", allSymbols);
        RabbitMQBaseBusConnection = SharedReferenceFinder.TryFind("gAPI.Core.ServiceBus.RabbitMQ.RabbitMQBaseBusConnection", allSymbols);
        AzureBaseBusConnection = SharedReferenceFinder.TryFind("gAPI.Core.ServiceBus.AzureServiceBus.AzureBaseBusConnection", allSymbols);

        FabricClient = SharedReferenceFinder.TryFind("gAPI.Core.Server.Fabric.BusFabricClient", allSymbols);
        SessionCache = SharedReferenceFinder.TryFind("gAPI.Core.Server.Collections.SessionCache", allSymbols);
        StreamingCache = SharedReferenceFinder.TryFind("gAPI.Core.Server.Collections.StreamingCache", allSymbols);
        ServiceSubscriptionCollection = SharedReferenceFinder.TryFind("gAPI.Core.Server.Collections.ServiceSubscriptionCollection", allSymbols);
        DeliveryMode = SharedReferenceFinder.Find("gAPI.Core.Enums.DeliveryMode", allSymbols);
    }

    public SharedReference BusConnectionCollection { get; }
    public SharedReference ServiceId { get; }
    public SharedReference ServiceMethodId { get; }
    public SharedReference RequestId { get; }
    public SharedReference ServiceBusConfig { get; }
    public SharedReference StreamingResponseDto { get; }
    public SharedReference BusSendRequestDto { get; }
    public SharedReference BusInvokeRequestDto { get; }
    public SharedReference BusRoutingDto { get; }
    public SharedReference IBaseBusConnection { get; }
    public SharedReference CancellationTokenSourceDispose { get; }
    public SharedReference BusToChoice { get; }
    public SharedReference BusConnectionId { get; }
    public SharedReference IHostedBusService { get; }
    public SharedReference IBusServiceSubscription { get; }
    public SharedReference? BaseFabricConnection { get; }
    public SharedReference? RabbitMQBaseBusConnection { get; }
    public SharedReference? AzureBaseBusConnection { get; }
    public SharedReference? FabricClient { get; }
    public SharedReference? SessionCache { get; }
    public SharedReference? StreamingCache { get; }
    public SharedReference? ServiceSubscriptionCollection { get; }
    public SharedReference DeliveryMode { get; }
}