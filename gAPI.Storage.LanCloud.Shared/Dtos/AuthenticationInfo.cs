namespace gAPI.Storage.LanCloud.Api.Models;

public record AuthenticationInfo(
    bool Required,
    string Realm);