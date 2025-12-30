using gAPI.AutoHub.Models;

namespace gAPI.AutoHub.Generators
{
    internal class SignalRHubGenerator : BaseGenerator
    {
        internal SignalRHubGenerator(ServiceContext dataModel)
        {
            DataModel = dataModel;

            Directory = dataModel.Config.Hubs_Destination.Directory;
            Namespace = dataModel.Config.Hubs_Destination.Namespace;

            Name = "SignalRHub";
            FileName = $"{Name}.g.cs";
        }

        public ServiceContext DataModel { get; }

        public void GenerateCode()
        {
            Reg("Microsoft.AspNetCore.SignalR");
            Code = @$"{GetNamespacesCode()}#nullable enable

namespace {Namespace};

public class {Name}(gAPI.Interfaces.IServerAuthenticationService authenticationService) : Hub
{{
    List<string> UserIds = [];

    public override async Task OnConnectedAsync()
    {{
        Console.WriteLine($""Client connected: {{Context.ConnectionId}}"");
        await base.OnConnectedAsync();
    }}

    public async Task InitializeAsync(Guid sessionId)
    {{
        await authenticationService.InitializeAsync(sessionId);
        var userId = await authenticationService.GetUserId();
        if (userId == null) return;

        UserIds.Add(userId);
        await Groups.AddToGroupAsync(Context.ConnectionId, userId);
        Console.WriteLine($""Client authenticated: {{Context.ConnectionId}} / {{userId}}"");
    }}

    public override async Task OnDisconnectedAsync(Exception? exception)
    {{
        foreach (var userId in UserIds)
        {{
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);
            Console.WriteLine($""Client removed from group: {{Context.ConnectionId}} / {{userId}}"");
        }}
        Console.WriteLine($""Client disconnected: {{Context.ConnectionId}}"");
        await base.OnDisconnectedAsync(exception);
    }}
}}";
        }
    }
}