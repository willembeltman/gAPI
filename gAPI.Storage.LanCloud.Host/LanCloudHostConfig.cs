using gAPI.Storage.LanCloud.Shared.Models;

namespace gAPI.Storage.LanCloud.Host;

public record LanCloudHostConfig(
    LocalShare[] Shares);