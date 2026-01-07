using System.Linq;

namespace gAPI.AutoSseClient.Generators
{
    internal class SseManagerGenerator : BaseGenerator
    {
        internal SseManagerGenerator(
            ServiceContext dataModel,
            ISseManagerGenerator iSseManager,
            SseClientGenerator sseClient)
        {
            DataModel = dataModel;
            ISseManager = iSseManager;
            SseClient = sseClient;

            Directory = dataModel.Config.HubClients_Destination.Directory;
            Namespace = dataModel.Config.HubClients_Destination.Namespace;

            Name = "SseManager";
            FileName = $"{Name}.g.cs";
        }

        public ServiceContext DataModel { get; }
        public ISseManagerGenerator ISseManager { get; }
        public SseClientGenerator SseClient { get; }

        public void GenerateCode()
        {
            Reg("Newtonsoft.Json");
            Reg("System.Collections.Concurrent");
            Reg("System.Collections.Immutable");
            Reg(ISseManager);
            Reg(SseClient);
            Reg(DataModel.IClientAuthenticationService);
            Reg(DataModel.SseManagerCollection);
            Reg(DataModel.SseServiceId);

            var arrays = string.Join(Environment.NewLine, DataModel.Interfaces
                .Select(i =>
                {
                    Reg(i);
                    return @$"
        private ImmutableArray<{i.Name}> {i.ApiName}s = [];";
                }));

            var subscibe = string.Join(Environment.NewLine, DataModel.Interfaces
                .Select(i =>
                {
                    Reg(i);
                    return @$"
        public async Task SubscribeAsync({i.Name} implementation)
        {{
            var serviceId = new {DataModel.SseServiceId}(nameof({i.Name}));
            var client = ServiceClients.AddOrUpdate(
                serviceId,
                serviceId =>
                {{
                    var client = new SseClient(ClientAuthenticationService, this, serviceId);
                    ImmutableInterlocked.Update(ref {i.ApiName}s, list => list.Add(implementation));
                    _ = Task.Run(client.ConnectAsync);
                    return client;
                }},
                (service, client) =>
                {{
                    ImmutableInterlocked.Update(ref {i.ApiName}s, list => list.Add(implementation));
                    return client;
                }});
        }}
        public async Task UnsubscribeAsync({i.Name} implementation)
        {{
            var serviceId = new {DataModel.SseServiceId}(nameof({i.Name}));
            ImmutableInterlocked.Update(ref {i.ApiName}s, list => list.Remove(implementation));
            if ({i.ApiName}s.Length == 0 &&
                ServiceClients.TryRemove(serviceId, out var client))
            {{
                await client.DisposeAsync();
            }}
        }}";
                }));

            var received = string.Join(Environment.NewLine, DataModel.Interfaces
                .Select(i =>
                {
                    Reg(i);
                    return $@"
                case ""{i.Name}"":
                    switch(message.ServiceMethodId.Value)
                    {{{string.Join(Environment.NewLine, i.Methods.Select(m => $@"
                        case ""{m.Name}"":
                            var {m.Name.ToLower()} = JsonConvert.DeserializeObject<{i.ApiName}_{m.Name}>(message.Data);
                            if ({m.Name.ToLower()} == null) return;
                            foreach (var item in {i.ApiName}s)
                            {{
                                await item.{m.Name}({m.Name.ToLower()}.message);
                            }}
                            break;"))}
                    }}
                    break;";
                }));

            var callbacks = string.Join(Environment.NewLine, DataModel.Interfaces
                .Select(i =>
                {
                    Reg(i);
                    return string.Join(Environment.NewLine, i.Methods
                        .Select(m =>
                        {
                            return @$"
        public class {i.ApiName}_{m.Name}
        {{{string.Join(Environment.NewLine, m.Arguments.Select(p =>
                            {
                                RegRange(p.ParameterType.Namespaces);
                                return @$"
            public {p.ParameterType.Name} {p.Name} {{ get; set; }}";
                            }))}
        }}
";
                        }));
                }));



            Code = @$"{GetNamespacesCode()}#nullable enable

namespace {Namespace}
{{
    public class {Name} : {ISseManager.Name}
    {{
        private readonly {DataModel.IClientAuthenticationService.Name} ClientAuthenticationService;
        private readonly {DataModel.SseManagerCollection.Name} SseManagerCollection;
        private readonly CancellationTokenSource Cts = new();
        private readonly ConcurrentDictionary<{DataModel.SseServiceId.Name}, {SseClient.Name}> ServiceClients = new();

{arrays}

        public SseManagerId Id {{ get; }}

        public SseManager(
            {DataModel.IClientAuthenticationService.Name} clientAuthenticationService,
            {DataModel.SseManagerCollection.Name} sseManagerCollection)
        {{
            ClientAuthenticationService = clientAuthenticationService;
            SseManagerCollection = sseManagerCollection;
            Id = SseManagerCollection.Add(this);
        }}{subscibe}

        public async Task MessageReceived(SseMessage message)
        {{
            switch (message.ServiceId.Value)
            {{{received}
            }}
        }}

        public async ValueTask DisposeAsync()
        {{
            Cts.Cancel();
            Cts.Dispose();
            foreach (var client in ServiceClients.Values)
            {{
                await client.DisposeAsync();
            }}
            SseManagerCollection.Remove(Id);
        }}
{callbacks}
    }}
}}";
        }
    }
}