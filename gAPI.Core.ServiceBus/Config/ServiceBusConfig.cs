namespace gAPI.Core.ServiceBus.Config;

public record ServiceBusConfig(
    int? MaxPackageSize,
    string QueueName,
    string ConnectionString)
{
    public string HostName { get; internal set; }
    public string UserName { get; internal set; }
    public string Password { get; internal set; }
}