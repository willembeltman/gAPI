namespace gAPI.Fabric.Server.Interfaces;

public interface IActor
{
    void EnqueueSend(long size);
    void EnqueueReceive(long size);
}
