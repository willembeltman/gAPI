using System.Collections.Immutable;

#nullable enable

namespace gAPI.Sse;

public sealed class SseServiceSnapshot<T>
{
    public readonly ImmutableArray<T> Services;
    public SseServiceSnapshot(ImmutableArray<T> services)
        => Services = services;
}