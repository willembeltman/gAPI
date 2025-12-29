using gAPI.Fabric.Types;

namespace gAPI.Fabric.Models;

public record Message(
    ServiceId ServiceId, 
    byte[] Data);
