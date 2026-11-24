using gAPI.Core.Dtos;
using System.Threading.Channels;

namespace gAPI.Core.Client.Logs;

public sealed class ClientLogQueue
{
    private readonly Channel<LogDto> _channel =
        Channel.CreateBounded<LogDto>(
            new BoundedChannelOptions(1024)
            {
                SingleReader = true,
                SingleWriter = false,
                FullMode = BoundedChannelFullMode.DropOldest,
                AllowSynchronousContinuations = false
            });

    public bool TryWrite(LogDto log)
        => _channel.Writer.TryWrite(log);

    public IAsyncEnumerable<LogDto> ReadAllAsync(CancellationToken ct)
        => _channel.Reader.ReadAllAsync(ct);
}
