namespace gAPI.Fabric;

public static class IPublishableExtensions
{
    public static void Publish(this IPublishable publishable, ServiceId serviceId, byte[] messageData)
    {
        foreach (var subscriber in publishable.AllSubscribers)
        {
            subscriber.BusClient.SendMessage(serviceId, messageData);
        }
    }
}