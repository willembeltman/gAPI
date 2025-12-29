//using gAPI.Fabric.Types;

//namespace gAPI.Fabric.Helpers;

//public static class IPublishableExtensions
//{
//    public static void Publish(this IPublishable publishable, ServiceId serviceId, UserId? userId, ScopeId? scopeId, byte[] messageData)
//    {
//        foreach (var subscriber in publishable.Subscriptions)
//        {
//            subscriber.Connection.SendMessage(serviceId, userId, scopeId, messageData);
//        }
//    }
//}