using gAPI.AutoBus.Helpers;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;

namespace gAPI.AutoBus.Models;

public class SharedReferences
{
    public SharedReferences(ServiceContext serviceContext, INamedTypeSymbol[] allSymbols)
    {
        ServiceId = SharedReferenceFinder.Find("gAPI.Core.Ids.ServiceId", allSymbols);
        ServiceMethodId = SharedReferenceFinder.Find("gAPI.Core.Ids.ServiceMethodId", allSymbols);
        RequestId = SharedReferenceFinder.Find("gAPI.Core.Ids.RequestId", allSymbols);

        ServiceBusConfig = SharedReferenceFinder.Find("gAPI.Core.ServiceBus.Config.ServiceBusConfig", allSymbols);
        BusRoutingDto = SharedReferenceFinder.Find("gAPI.Core.Dtos.BusRoutingDto", allSymbols);
        BusSendRequestDto = SharedReferenceFinder.Find("gAPI.Core.Dtos.BusSendRequestDto", allSymbols);
        BusInvokeRequestDto = SharedReferenceFinder.Find("gAPI.Core.Dtos.BusInvokeRequestDto", allSymbols);
        StreamingResponseDto = SharedReferenceFinder.Find("gAPI.Core.Dtos.StreamingResponseDto", allSymbols);
    
        IBaseBusConnection = SharedReferenceFinder.Find("gAPI.Core.Interfaces.IBaseBusConnection", allSymbols);

        BusToChoice = SharedReferenceFinder.Find("gAPI.Core.Enums.BusToChoice", allSymbols);
        BusConnectionId = SharedReferenceFinder.Find("gAPI.Core.Ids.BusConnectionId", allSymbols);

        BaseFabricConnection = SharedReferenceFinder.TryFind("gAPI.Core.ServiceBus.Fabric.BaseFabricConnection", allSymbols);
        BaseRabbitMQConnection = SharedReferenceFinder.TryFind("gAPI.Core.ServiceBus.RabbitMQ.BaseRabbitMQConnection", allSymbols);
        BaseAzureConnection = SharedReferenceFinder.TryFind("gAPI.Core.ServiceBus.AzureServiceBus.BaseAzureConnection", allSymbols);
        
        IFabricClient = SharedReferenceFinder.TryFind("gAPI.Core.Server.Interfaces.IFabricClient", allSymbols);

    }

    public SharedReference ServiceId { get; }
    public SharedReference ServiceMethodId { get; }
    public SharedReference RequestId { get; }
    public SharedReference ServiceBusConfig { get; }
    public SharedReference StreamingResponseDto { get; }
    public SharedReference BusSendRequestDto { get; }
    public SharedReference BusInvokeRequestDto { get; }
    public SharedReference BusRoutingDto { get; }
    public SharedReference IBaseBusConnection { get; }
    public SharedReference BusToChoice { get; }
    public SharedReference BusConnectionId { get; }
    public SharedReference? BaseFabricConnection { get; }
    public SharedReference? BaseRabbitMQConnection { get; }
    public SharedReference? BaseAzureConnection { get; }
    public SharedReference? IFabricClient { get; }
}