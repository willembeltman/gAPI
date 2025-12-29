using gAPI.Fabric.Types;

namespace gAPI.Fabric.Helpers;

public static class IPublishableExtensions
{
    public static void Publish(this IPublishable publishable, ServiceId serviceId, byte[] messageData)
    {
        foreach (var subscriber in publishable.Subscriptions)
        {
            subscriber.BusClient.SendMessage(serviceId, messageData);
        }
    }
}