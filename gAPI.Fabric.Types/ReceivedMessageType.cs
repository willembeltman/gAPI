namespace gAPI.Fabric.Types;

public enum ReceivedMessageType
{
    Subscribe = 1,
    UnSubscribe = 2,
    PublishToAll = 3,
    PublishToUser = 4,
    PublishToScope = 5,
}