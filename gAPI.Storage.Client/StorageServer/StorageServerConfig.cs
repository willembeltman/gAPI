using System;
using System.Collections.Generic;
using System.Text;

namespace gAPI.Storage.Client.StorageServer;

public record StorageServerConfig(
    double UrlTimeoutSeconds,
    double AuthenticateTimeoutMinutes,
    int? MaxPackageSize = null);