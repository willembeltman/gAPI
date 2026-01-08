using gAPI.Fabric;
using gAPI.FabricNode.Collections;
using gAPI.Ids;
using gAPI.Sse;
using System.Net.Sockets;
using System.Threading.Channels;

namespace gAPI.FabricNode
{
    public sealed class FabricHost
    {
        private readonly FabricManager Manager;
        private readonly TcpClient TcpClient;
        private readonly FabricHostCollection Connections;
        private readonly CancellationTokenSource Cts;
        private readonly NetworkStream Stream;
        private readonly Channel<Action<BinaryWriter>> SendQueue;
        private readonly FabricConverter fc = new();

        public FabricHostId Id { get; }

        public FabricHost(
            FabricManager manager, 
            TcpClient tcpClient,
            FabricHostCollection connections)
        {
            Manager = manager;
            TcpClient = tcpClient;
            Connections = connections;
            Cts = new CancellationTokenSource();
            Stream = tcpClient.GetStream();
            SendQueue = Channel.CreateUnbounded<Action<BinaryWriter>>();
            Id = Connections.AddConnection(this);
        }

        public void Start()
        {
            _ = Task.Run(ReceiveLoop);
            _ = Task.Run(SendLoop);
        }

        public void SendMessageToClient(SseMessage message)
        {
            Enqueue(w =>
            {
                fc.WriteHostToClientMessageType(w, FabricHostToClientMessageEnum.SendSseMessageToClient);
                fc.WriteServiceId(w, message.ServiceId);
                fc.WriteServiceMethodId(w, message.ServiceMethodId);
                fc.WriteNullableUserId(w, message.UserId);
                fc.WriteNullableSessionId(w, message.SessionId);
                fc.WriteMessageData(w, message.Data);
            });
        }
        private void Enqueue(Action<BinaryWriter> write)
        {
            SendQueue.Writer.TryWrite(write);
        }
        private async Task SendLoop()
        {
            var writer = new BinaryWriter(Stream);
            fc.WriteFabricHostId(writer, Id);
            await foreach (var item in SendQueue.Reader.ReadAllAsync(Cts.Token))
            {
                item(writer);
                writer.Flush();
                if (Cts.IsCancellationRequested) break;
            }
            await DisposeAsync();
        }

        private async Task ReceiveLoop()
        {
            Console.WriteLine();
            Console.WriteLine($"FabricHost {Id} started");
            Console.WriteLine();

            try
            {
                var r = new BinaryReader(Stream);
                while (!Cts.IsCancellationRequested)
                {
                    switch (fc.ReadClientToHostMessageType(r))
                    {
                        case FabricClientToHostMessageEnum.Subscribe:
                            Manager.Subscribe(
                                this,
                                fc.ReadServiceId(r),
                                fc.ReadUserId(r),
                                fc.ReadSessionId(r));
                            break;
                        case FabricClientToHostMessageEnum.UnSubscribe:
                            Manager.Unsubscribe(
                                this,
                                fc.ReadServiceId(r),
                                fc.ReadUserId(r),
                                fc.ReadSessionId(r));
                            break;
                        case FabricClientToHostMessageEnum.Publish:
                            Manager.Publish(
                                this,
                                fc.ReadServiceId(r),
                                fc.ReadServiceMethodId(r),
                                fc.ReadNullableUserId(r),
                                fc.ReadNullableSessionId(r),
                                fc.ReadMessageData(r));
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine($"FabricClient #{Id.Value}: Exception occured, restarting fabric client");
                Console.WriteLine($"{ex}");
                Console.WriteLine();
            }
            await DisposeAsync();
        }

        public async ValueTask DisposeAsync()
        {
            Cts.Dispose();
            Connections.RemoveConnection(Id);

            await Stream.DisposeAsync();
            TcpClient.Dispose();
        }
    }
}