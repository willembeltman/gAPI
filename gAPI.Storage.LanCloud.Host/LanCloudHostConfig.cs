using gAPI.Core.Client.Config;
using gAPI.Storage.LanCloud.Shared.Models;

namespace gAPI.Storage.LanCloud.Host;

public record LanCloudHostConfig(
    string ApiBackendUrl,
    string WssBackendUrl,
    LocalShare[] Shares)
    : ClientConfig(ApiBackendUrl, WssBackendUrl);