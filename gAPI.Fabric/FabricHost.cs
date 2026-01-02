using gAPI.Fabric;
using gAPI.FabricClient.Models;
using gAPI.Sse;
using System.Net.Sockets;
using System.Threading.Channels;

namespace gAPI.FabricClient
{
    public sealed class FabricHost
    {
        private readonly ConnectionManager Manager;
        private readonly TcpClient TcpClient;
        private readonly CancellationTokenSource Cts;
        private readonly NetworkStream Stream;
        private readonly Channel<Action<BinaryWriter>> channel;
        private readonly FabricConverter fc = new();

        public FabricHostId Id { get; }

        public FabricHost(ConnectionManager manager, TcpClient tcpClient)
        {
            Id = manager.AddConnection(this);

            Manager = manager;
            TcpClient = tcpClient;
            Cts = new CancellationTokenSource();
            Stream = tcpClient.GetStream();
            channel = Channel.CreateUnbounded<Action<BinaryWriter>>();
        }

        public async Task RunAsync()
        {
            _ = Task.Run(ReceiveLoop);
            _ = Task.Run(SendLoop);
        }
        private async Task ReceiveLoop()
        {
            Console.WriteLine();
            Console.WriteLine($"FabricHost #{Id.Value} started");
            Console.WriteLine();

            var r = new BinaryReader(Stream);
            while (!Cts.IsCancellationRequested)
            {
                switch (fc.ReadClientToHostMessageType(r))
                {
                    case FabricClientToHostMessageEnum.Subscribe:
                        Manager.Subscribe(
                            fc.ReadServiceId(r),
                            fc.ReadUserId(r),
                            fc.ReadSessionId(r),
                            this);
                        break;
                    case FabricClientToHostMessageEnum.UnSubscribe:
                        Manager.UnSubscribe(
                            fc.ReadServiceId(r),
                            fc.ReadUserId(r),
                            fc.ReadSessionId(r),
                            this);
                        break;
                    case FabricClientToHostMessageEnum.Publish:
                        Manager.Publish(
                            fc.ReadServiceId(r),
                            fc.ReadNullableUserId(r),
                            fc.ReadNullableSessionId(r),
                            fc.ReadMessageData(r));
                        break;
                }
            }
            await DisposeAsync();
        }
        private async Task SendLoop()
        {
            var w = new BinaryWriter(Stream);
            fc.WriteFabricHostId(w, Id);
            await foreach (var item in channel.Reader.ReadAllAsync(Cts.Token))
            {
                item(w);
                w.Flush();
                if (Cts.IsCancellationRequested) break;
            }
            await DisposeAsync();
        }

        public void SendMessage(SseMessage message)
        {
            //Console.WriteLine($"{DateTime.Now:HH:mm:ss.FFF}: FabricHost.SendMessage");
            Enqueue(w =>
            {
                fc.WriteHostToClientMessageType(w, FabricHostToClientMessageEnum.SendMessage);
                fc.WriteServiceId(w, message.ServiceId);
                fc.WriteNullableUserId(w, message.UserId);
                fc.WriteNullableSessionId(w, message.SessionId);
                fc.WriteMessageData(w, message.Data);
            });
        }
        private void Enqueue(Action<BinaryWriter> write)
        {
            channel.Writer.TryWrite(write);
        }

        public async ValueTask DisposeAsync()
        {
            Cts.Dispose();
            Manager.RemoveConnection(this);

            await Stream.DisposeAsync();
            TcpClient.Dispose();
        }
    }
}