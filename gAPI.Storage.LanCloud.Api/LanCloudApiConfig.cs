using gAPI.Storage.LanCloud.Shared.Models;

namespace gAPI.Storage.LanCloud.Api;

public record LanCloudApiConfig(
    LocalShare LocalShare,
    string? CertificateFilename = null);