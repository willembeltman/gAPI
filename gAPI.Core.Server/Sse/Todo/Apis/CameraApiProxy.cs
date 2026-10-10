//using Bsd.Public.Shared.Dtos;
//using Bsd.Public.Shared.Interfaces;
//using gAPI.Core.Attributes;
//using gAPI.Core.Dtos;
//using gAPI.Core.Ids;
//using gAPI.Core.Interfaces;
//using gAPI.Core.Serializers;
//using gAPI.Core.Server.Fabric;

//namespace gAPI.Generated;

//[IsApiProxy]
//public class CameraApiProxy(
//    FabricClient ___fabricClient,
//    IServerAuthenticationService ___authenticationService,
//    ILoggerFactory ___loggerFactory,
//    FabricConnectionId ___fabricConnectionId,
//    ClientConnectionId ___clientConnectionId) 
//    : ICameraApi
//{
//    readonly CancellationTokenSource ___Cts = new();
//    readonly ILogger ___Logger = ___loggerFactory.CreateLogger<CameraHub>();
//    readonly ServiceId ___ServiceId = new("ICameraHub");

//    public async Task SendVideoChunkAsync(string hash, string salt, Guid recordingId, CameraInfo info, ReadOnlyMemory<byte> dataChunk, CancellationToken ct = default)
//    {
//        if (___Logger.IsEnabled(LogLevel.Trace))
//            ___Logger.LogTrace("SendVideoChunkAsync({hash}, {salt}, {recordingId}, {info}, {dataChunk})", hash, salt, recordingId, info, dataChunk);

//        using var ___activityCts = CancellationTokenSource.CreateLinkedTokenSource(___Cts.Token, ct);

//        var ___routing = new ServerRoutingDto(
//            ___fabricClient.FabricManagerId,
//            ___fabricClient.FabricConnectionId,
//            RequestId.New(),
//            ___ServiceId,
//            new("SendVideoChunkAsync"),
//            ___fabricConnectionId,
//            ___clientConnectionId
//        );

//        {
//            await ___fabricClient.Send_SendRequestServerToServer_ToFabricAsync( 
//                ___authenticationService,
//                ___routing,
//                ___SendVideoChunkAsync_Serializer(hash, salt, recordingId, info, dataChunk),
//                ___activityCts.Token);
//        }
//    }
//    private byte[] ___SendVideoChunkAsync_Serializer(string hash, string salt, Guid recordingId, CameraInfo info, ReadOnlyMemory<byte> dataChunk)
//    {
//        var ___offset = 0;
//        PrimitivesSpanSerializer.LengthString(ref ___offset, hash);
//        PrimitivesSpanSerializer.LengthString(ref ___offset, salt);
//        GuidSerializer.GetMessageLength(ref ___offset, recordingId);
//        CameraInfoSpanSerializer.Length(ref ___offset, info);
//        PrimitivesSpanSerializer.LengthByteReadOnlyMemory(ref ___offset, dataChunk);
//        var ___buffer = new byte[___offset];
//        var ___span = new Span<byte>(___buffer);
//        var ___length = ___offset;
//        ___offset = 0;
//        PrimitivesSpanSerializer.WriteString(ref ___span, ref ___offset, hash);
//        PrimitivesSpanSerializer.WriteString(ref ___span, ref ___offset, salt);
//        GuidSerializer.WriteGuid(ref ___span, ref ___offset, recordingId);
//        CameraInfoSpanSerializer.Write(ref ___span, ref ___offset, info);
//        PrimitivesSpanSerializer.WriteByteReadOnlyMemory(ref ___span, ref ___offset, dataChunk);
//        if (___length != ___offset)
//            throw new Exception($"Binary length doesn't match: {___length} != {___offset}");
//        return ___buffer;
//    }

//    public async Task<CameraRecording[]> ListActiveRecordings(CancellationToken ct = default)
//    {
//        if (___Logger.IsEnabled(LogLevel.Trace))
//            ___Logger.LogTrace("ListActiveRecordings()");

//        using var ___activityCts = CancellationTokenSource.CreateLinkedTokenSource(___Cts.Token, ct);

//        var ___routing = new ServerRoutingDto(
//            ___fabricClient.FabricManagerId,
//            ___fabricClient.FabricConnectionId,
//            RequestId.New(),
//            ___ServiceId,
//            new("ListActiveRecordings"),
//            ___fabricConnectionId,
//            ___clientConnectionId
//        );


//        CameraRecording[] ___result = default!;

//        {
//            var ___responses = ___fabricClient.Send_InvokeRequestServerToServer_ToFabricAsync(
//                ___authenticationService,
//                ___routing,
//                ___ListActiveRecordings_Serializer(),
//                ___activityCts.Token);

//            await foreach (var item in ___responses)
//            {
//                var ___offset = 0;
//                var ___span = new Span<byte>(item);
//                ___result = BuildListCameraRecording(___span, ref ___offset, PrimitivesSpanSerializer.ReadInt32(___span, ref ___offset));
//            }
//        }

//        return ___result;
//    }
//    private byte[] ___ListActiveRecordings_Serializer()
//    {
//        return [];
//    }

//    public async Task<CameraRecordingListener> SubscribeToRecording(CameraRecording recording, CancellationToken ct = default)
//    {
//        if (___Logger.IsEnabled(LogLevel.Trace))
//            ___Logger.LogTrace("SubscribeToRecording({recording})", recording);

//        using var ___activityCts = CancellationTokenSource.CreateLinkedTokenSource(___Cts.Token, ct);

//        var ___routing = new ServerRoutingDto(
//            ___fabricClient.FabricManagerId,
//            ___fabricClient.FabricConnectionId,
//            RequestId.New(),
//            ___ServiceId,
//            new("SubscribeToRecording"),
//            ___fabricConnectionId,
//            ___clientConnectionId
//        );


//        CameraRecordingListener ___result = default!;

//        {
//            var ___responses = ___fabricClient.Send_InvokeRequestServerToServer_ToFabricAsync(
//                ___authenticationService,
//                ___routing,
//                ___SubscribeToRecording_Serializer(recording),
//                ___activityCts.Token);

//            await foreach (var item in ___responses)
//            {
//                var ___offset = 0;
//                var ___span = new Span<byte>(item);
//                ___result = CameraRecordingListenerSpanSerializer.ReadCameraRecordingListener(___span, ref ___offset);
//            }
//        }

//        return ___result;
//    }
//    private byte[] ___SubscribeToRecording_Serializer(CameraRecording recording)
//    {
//        var ___offset = 0;
//        CameraRecordingSpanSerializer.Length(ref ___offset, recording);
//        var ___buffer = new byte[___offset];
//        var ___span = new Span<byte>(___buffer);
//        var ___length = ___offset;
//        ___offset = 0;
//        CameraRecordingSpanSerializer.Write(ref ___span, ref ___offset, recording);
//        if (___length != ___offset)
//            throw new Exception($"Binary length doesn't match: {___length} != {___offset}");
//        return ___buffer;
//    }

//    public async Task UnsubscribeFromListener(CameraRecordingListener currentListener, CancellationToken ct = default)
//    {
//        if (___Logger.IsEnabled(LogLevel.Trace))
//            ___Logger.LogTrace("UnsubscribeFromListener({currentListener})", currentListener);

//        using var ___activityCts = CancellationTokenSource.CreateLinkedTokenSource(___Cts.Token, ct);

//        var ___routing = new ServerRoutingDto(
//            ___fabricClient.FabricManagerId,
//            ___fabricClient.FabricConnectionId,
//            RequestId.New(),
//            ___ServiceId,
//            new("UnsubscribeFromListener"),
//            ___fabricConnectionId,
//            ___clientConnectionId
//        );

//        {
//            await ___fabricClient.Send_SendRequestServerToServer_ToFabricAsync(
//                ___authenticationService,
//                ___routing,
//                ___UnsubscribeFromListener_Serializer(currentListener),
//                ___activityCts.Token);
//        }
//    }
//    private byte[] ___UnsubscribeFromListener_Serializer(CameraRecordingListener currentListener)
//    {
//        var ___offset = 0;
//        CameraRecordingListenerSpanSerializer.Length(ref ___offset, currentListener);
//        var ___buffer = new byte[___offset];
//        var ___span = new Span<byte>(___buffer);
//        var ___length = ___offset;
//        ___offset = 0;
//        CameraRecordingListenerSpanSerializer.Write(ref ___span, ref ___offset, currentListener);
//        if (___length != ___offset)
//            throw new Exception($"Binary length doesn't match: {___length} != {___offset}");
//        return ___buffer;
//    }

//    public async Task Pong(CameraRecordingListener listener, string pongCode, CancellationToken ct = default)
//    {
//        if (___Logger.IsEnabled(LogLevel.Trace))
//            ___Logger.LogTrace("Pong({listener}, {pongCode})", listener, pongCode);

//        using var ___activityCts = CancellationTokenSource.CreateLinkedTokenSource(___Cts.Token, ct);

//        var ___routing = new ServerRoutingDto(
//            ___fabricClient.FabricManagerId,
//            ___fabricClient.FabricConnectionId,
//            RequestId.New(),
//            ___ServiceId,
//            new("Pong"),
//            ___fabricConnectionId,
//            ___clientConnectionId
//        );

//        {
//            await ___fabricClient.Send_SendRequestServerToServer_ToFabricAsync(
//                ___authenticationService,
//                ___routing,
//                ___Pong_Serializer(listener, pongCode),
//                ___activityCts.Token);
//        }
//    }
//    private byte[] ___Pong_Serializer(CameraRecordingListener listener, string pongCode)
//    {
//        var ___offset = 0;
//        CameraRecordingListenerSpanSerializer.Length(ref ___offset, listener);
//        PrimitivesSpanSerializer.LengthString(ref ___offset, pongCode);
//        var ___buffer = new byte[___offset];
//        var ___span = new Span<byte>(___buffer);
//        var ___length = ___offset;
//        ___offset = 0;
//        CameraRecordingListenerSpanSerializer.Write(ref ___span, ref ___offset, listener);
//        PrimitivesSpanSerializer.WriteString(ref ___span, ref ___offset, pongCode);
//        if (___length != ___offset)
//            throw new Exception($"Binary length doesn't match: {___length} != {___offset}");
//        return ___buffer;
//    }


//    static CameraRecording[] BuildListCameraRecording(ReadOnlySpan<byte> ___span, ref int ___offset, int count)
//    {
//        var list = new List<CameraRecording>(count);
//        for (int i = 0; i < count; i++)
//        {
//            var item = CameraRecordingSpanSerializer.ReadCameraRecording(___span, ref ___offset);
//            list.Add(item);
//        }
//        return [.. list];
//    }

//    public void Dispose()
//    {
//        ___Cts.Cancel();
//        ___Cts.Dispose();

//        GC.SuppressFinalize(this);
//    }
//}