namespace gAPI.FabricNode;

public sealed class CountingDuplexStream : Stream
{
    private readonly Stream _inner;

    public long BytesWritten { get; private set; }
    public long BytesRead { get; private set; }

    public CountingDuplexStream(Stream inner)
    {
        _inner = inner;
    }

    // WRITE
    public override void Write(byte[] buffer, int offset, int count)
    {
        BytesWritten += count;
        _inner.Write(buffer, offset, count);
    }

    public override async ValueTask WriteAsync(
        ReadOnlyMemory<byte> buffer,
        CancellationToken cancellationToken = default)
    {
        BytesWritten += buffer.Length;
        await _inner.WriteAsync(buffer, cancellationToken);
    }

    // READ
    public override int Read(byte[] buffer, int offset, int count)
    {
        var read = _inner.Read(buffer, offset, count);
        BytesRead += read;
        return read;
    }

    public override async ValueTask<int> ReadAsync(
        Memory<byte> buffer,
        CancellationToken cancellationToken = default)
    {
        var read = await _inner.ReadAsync(buffer, cancellationToken);
        BytesRead += read;
        return read;
    }

    // passthrough
    public override bool CanRead => _inner.CanRead;
    public override bool CanSeek => _inner.CanSeek;
    public override bool CanWrite => _inner.CanWrite;
    public override long Length => _inner.Length;
    public override long Position { get => _inner.Position; set => _inner.Position = value; }
    public override void Flush() => _inner.Flush();
    public override Task FlushAsync(CancellationToken cancellationToken) => _inner.FlushAsync(cancellationToken);
    public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);
    public override void SetLength(long value) => _inner.SetLength(value);
}
