namespace gAPI.Fabric.Interfaces;

public interface IActor
{
    void EnqueueSend(long size);
    void EnqueueReceive(long size);
}
