namespace gAPI.FabricNode.Models;

public interface IActor
{
    void EnqueueSend(long size);
    void EnqueueReceive(long size);
}
