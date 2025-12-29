namespace gAPI.Fabric;

public enum ServerMessageTypeEnum
{
    Ping = 0,
    Subscribe = 1,
    UnSubscribe = 2,
    PublishToAll = 3,
    PublishToUser = 4,
    PublishToScope = 5,
}