using gAPI.Core.Dtos;
using gAPI.Core.Helpers;
using gAPI.Core.Server.Entities;
using gAPI.Generated;
using gAPI.Storage.LanCloud.Api.Collections;
using gAPI.Storage.LanCloud.Api.Interfaces;
using gAPI.Storage.LanCloud.Api.Models;
using gAPI.Storage.LanCloud.Shared.Dtos;
using gAPI.Storage.LanCloud.Shared.Interfaces;
using gAPI.Storage.LanCloud.Shared.Models;
using System.Runtime.CompilerServices;

namespace gAPI.Storage.LanCloud.Api.Services;

internal class FileSystem(
    IClientContext clientContext,
    RespondedEntryCollection entryCollection,
    LanCloudApiConfig apiConfig) 
    : IFileSystemApi, IFileSystemDirect

{
    LocalShare LocalShare => apiConfig.LocalShare;

    public async Task<AuthenticationInfo> GetAuthenticationInfo(CancellationToken ct)
    {
        return new AuthenticationInfo(
            Required: false,
            Realm: "LanCloud");
    }

    public async Task<AuthStateUserDto?> AuthenticateUser(string? userName, string? password, CancellationToken ct)
    {
        return new AuthStateUserDto()
        {
            Email = userName ?? "",
            UserName = userName ?? ""
        };
    }

    public Task CreateDirectory(string path, CancellationToken ct)
    {
        return LocalShare.CreateDirectory(path, ct);
    }

    public async Task Delete(string path, CancellationToken ct)
    {
        if (await LocalShare.Exist(path, ct))
        {
            await LocalShare.Delete(path, ct);
            return;
        }

        throw new InvalidOperationException(
            "Remote files are read-only.");
    }

    public async Task Move(string sourcePath, string destinationPath, CancellationToken ct)
    {
        if (!await LocalShare.Exist(sourcePath, ct))
        {
            throw new InvalidOperationException(
                "Remote files are read-only.");
        }

        await LocalShare.Move(
            sourcePath,
            destinationPath,
            ct);
    }

    public async Task<FileSystemEntry?> Get(string path, CancellationToken ct)
    {
        path = RespondedEntryCollection.Normalize(path);

        if (string.IsNullOrEmpty(path))
        {
            var rootFsEntry = new FileSystemEntry(
                "",
                "",
                true,
                0,
                DateTime.UtcNow,
                DateTime.UtcNow);

            var rootShareEntry = new HubEntryDto("", "", true, 0, DateTime.Now, DateTime.Now, null);

            var rootEntry = new RespondedEntry(
                rootFsEntry,
                rootShareEntry,
                "");

            entryCollection.Responded("", rootEntry);

            return rootFsEntry;
        }

        var remoteTask = clientContext.HostHub
            .ToAll
            .Get(path, ct)
            .ToArrayAsync(ct)
            .AsTask();

        var localTask = LocalShare
            .Get(path, null, ct)
            .ToArrayAsync(ct)
            .AsTask();

        await Task.WhenAll(remoteTask, localTask);

        var allShareFiles = remoteTask.Result.Concat(localTask.Result);

        var shareFile = allShareFiles
            .OrderByDescending(x => x.GetLastDate())
            .FirstOrDefault();

        if (shareFile == null)
            return null;

        return await CreateFileSystemEntry(
            path,
            path,
            shareFile,
            ct);
    }

    public async IAsyncEnumerable<FileSystemEntry> ListDirectory(string path, [EnumeratorCancellation] CancellationToken ct)
    {
        path = RespondedEntryCollection.Normalize(path);

        var remoteTask = clientContext.HostHub
            .ToAll
            .ListDirectory(path, ct)
            .ToArrayAsync(ct)
            .AsTask();

        var localTask = LocalShare
            .ListDirectory(path, null, ct)
            .ToArrayAsync(ct)
            .AsTask();

        await Task.WhenAll(remoteTask, localTask);

        var files = remoteTask.Result.Concat(localTask.Result);

        foreach (var shareFile in files
            .GroupBy(x => x.Path)
            .Select(x => x.OrderByDescending(y => y.GetLastDate()).First()))
        {
            yield return await CreateFileSystemEntry(
                shareFile.Path,
                shareFile.Path,
                shareFile,
                ct);
        }
    }

    public async Task<Stream?> OpenRead(string path, CancellationToken ct)
    {
        if (!entryCollection.TryGet(path, out var entry) || entry == null)
        {
            var fetched = await Get(path, ct);
            if (fetched == null || !entryCollection.TryGet(path, out entry) || entry == null)
            {
                return null;
            }
        }

        IAsyncEnumerable<DataChunkDto> OpenChunks(long startOffset, CancellationToken streamCt)
        {
            if (entry.ShareEntryDto.SessionId == null)
            {
                return LocalShare.ReadFile(entry.Path, startOffset, streamCt);
            }

            return clientContext.HostHub
                .ToSession(entry.ShareEntryDto.SessionId)
                .ReadFile(entry.Path, startOffset, streamCt);
        }

        return new DataChunkStreamSeekableReader(OpenChunks, entry.FileSystemEntry.Size, ct);
    }
    public Task Write(string path, long startOffset, Stream stream, CancellationToken ct)
    {
        return LocalShare.Write(path, startOffset, stream, ct);
    }
    public Task Append(string path, Stream stream, CancellationToken ct)
    {
        return LocalShare.Append(path, stream, ct);
    }

    public async IAsyncEnumerable<ReadOnlyMemory<byte>> OpenReadReadOnlyMemoryByte(
        string path,
        long startOffset,
        [EnumeratorCancellation] CancellationToken ct)
    {
        // Deze 8MB array wordt EXACT één keer aangemaakt voor de gehele stream
        var buffer = new byte[8 * 1024 * 1024];

        using var stream = await OpenRead(path, ct);
        if (stream == null)
            yield break;

        while (!ct.IsCancellationRequested)
        {
            // We lezen direct in het Memory-venster van onze vaste buffer
            var read = await stream.ReadAsync(buffer.AsMemory(), ct);
            if (read <= 0)
                yield break;

            // MAGIE: buffer.AsMemory(0, read) maakt GEEN kopie van de bytes! 
            // Het geeft gAPI puur een 'kijkgat' (pointer + lengte) in de bestaande array.
            yield return buffer.AsMemory(0, read);
        }
    }


    public async IAsyncEnumerable<byte[]> OpenReadByteArray(string path, long startOffset, [EnumeratorCancellation] CancellationToken ct)
    {
        var buffer = new byte[8 * 1024 * 1024];
        using var stream = await OpenRead(path, ct);
        if (stream == null)
            yield break;
        while (true)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(), ct);
            if (read <= 0)
                yield break;

            // Let op: we returnen hier een slice. Voor gAPI is het vaak veiliger 
            // om buffer[..read] te kopiëren als gAPI de array asynchroon vasthoudt, 
            // maar als gAPI hem direct wegschrijft is dit prima!
            yield return buffer[..read];
        }
    }
    public async Task WriteAsyncEnumerableByte(string path, long startOffset, IAsyncEnumerable<byte[]> buffer, CancellationToken ct)
    {
        var pipe = new System.IO.Pipelines.Pipe();
        var writeTask = LocalShare.Write(path, startOffset, pipe.Reader.AsStream(), ct);

        try
        {
            await foreach (var chunk in buffer.WithCancellation(ct))
            {
                await pipe.Writer.WriteAsync(chunk, ct);
            }
            await pipe.Writer.CompleteAsync();
        }
        catch (Exception ex)
        {
            await pipe.Writer.CompleteAsync(ex);
            throw;
        }

        await writeTask;
    }
    public async Task AppendAsyncEnumerableByte(string path, IAsyncEnumerable<byte[]> buffer, CancellationToken ct)
    {
        var pipe = new System.IO.Pipelines.Pipe();
        var appendTask = LocalShare.Append(path, pipe.Reader.AsStream(), ct);

        try
        {
            await foreach (var chunk in buffer.WithCancellation(ct))
            {
                await pipe.Writer.WriteAsync(chunk, ct);
            }
            await pipe.Writer.CompleteAsync();
        }
        catch (Exception ex)
        {
            await pipe.Writer.CompleteAsync(ex);
            throw;
        }

        await appendTask;
    }

    public Task WriteByteArray(string path, long startOffset, byte[] buffer, CancellationToken ct)
    {
        return WriteReadOnlyMemory(path, startOffset, buffer.AsMemory(), ct);
    }
    public Task AppendByteArray(string path, byte[] buffer, CancellationToken ct)
    {
        return AppendReadOnlyMemory(path, buffer.AsMemory(), ct);
    }

    public async Task WriteReadOnlyMemory(string path, long startOffset, ReadOnlyMemory<byte> buffer, CancellationToken ct)
    {
        using var memStream = new ReadOnlyMemoryStream(buffer);
        await LocalShare.Write(path, startOffset, memStream, ct);
    }
    public async Task AppendReadOnlyMemory(string path, ReadOnlyMemory<byte> buffer, CancellationToken ct)
    {
        using var memStream = new ReadOnlyMemoryStream(buffer);
        await LocalShare.Append(path, memStream, ct);
    }


    private async Task<FileSystemEntry> CreateFileSystemEntry(
        string visiblePath,
        string readPath,
        HubEntryDto shareFile,
        CancellationToken ct)
    {
        var fsFile = new FileSystemEntry(
            GetName(visiblePath, shareFile.Name),
            visiblePath,
            shareFile.IsDirectory,
            shareFile.Size,
            shareFile.Created,
            shareFile.LastModified);
        var entry = new RespondedEntry(fsFile, shareFile, readPath);
        entryCollection.Responded(visiblePath, entry);
        return fsFile;
    }

    private static string GetName(string path, string fallback)
    {
        path = RespondedEntryCollection.Normalize(path);
        if (string.IsNullOrEmpty(path))
            return fallback;

        var slash = path.LastIndexOf('/');
        return slash < 0 ? path : path[(slash + 1)..];
    }
    private sealed class ReadOnlyMemoryStream : Stream
    {
        private readonly ReadOnlyMemory<byte> _memory;
        private int _position;

        public ReadOnlyMemoryStream(ReadOnlyMemory<byte> memory) => _memory = memory;

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;
        public override long Length => _memory.Length;
        public override long Position { get => _position; set => _position = (int)value; }

        public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));

        public override int Read(Span<byte> buffer)
        {
            var remaining = _memory.Length - _position;
            if (remaining <= 0) return 0;

            var toRead = Math.Min(buffer.Length, (int)remaining);
            _memory.Span.Slice(_position, toRead).CopyTo(buffer);
            _position += toRead;
            return toRead;
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            var remaining = _memory.Length - _position;
            if (remaining <= 0) return ValueTask.FromResult(0);

            var toRead = Math.Min(buffer.Length, (int)remaining);
            _memory.Slice(_position, toRead).CopyTo(buffer);
            _position += toRead;
            return ValueTask.FromResult(toRead);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            _position = origin switch
            {
                SeekOrigin.Begin => (int)offset,
                SeekOrigin.Current => _position + (int)offset,
                SeekOrigin.End => _memory.Length + (int)offset,
                _ => _position
            };
            return _position;
        }

        public override void Flush() { }
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

}
