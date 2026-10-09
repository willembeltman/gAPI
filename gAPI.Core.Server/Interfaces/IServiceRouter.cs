//using gAPI.Core.Dtos;
//using gAPI.Core.Server.Fabric;

//namespace gAPI.Core.Server.Interfaces;

//public interface IServiceRouter
//{
//    Task SendRequestAsync(FabricClient fabricClient, ServerSendRequestDto message, CancellationToken ct);
//    IAsyncEnumerable<byte[]> InvokeRequestAsync(FabricClient fabricClient, ServerInvokeRequestDto message, CancellationToken ct);
//    Task SendRequestCancelled(FabricClient fabricClient, ServerSendRequestCancelledDto message, CancellationToken ct);
//    Task InvokeRequestCancelled(FabricClient fabricClient, ServerInvokeRequestCancelledDto message, CancellationToken ct);
//}