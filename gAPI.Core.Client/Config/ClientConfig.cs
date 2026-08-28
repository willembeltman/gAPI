namespace gAPI.Core.Client.Config;

public record ClientConfig(
    string ApiBackendUrl, 
    string? WssBackendUrl);
