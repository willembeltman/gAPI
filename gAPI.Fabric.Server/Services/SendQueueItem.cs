using gAPI.Fabric.Server.Interfaces;

namespace gAPI.Fabric.Server.Services;

public record SendQueueItem(Action<BinaryWriter> write, IActor actor);