namespace gAPI.AutoSse.Generators
{
    internal class SseHostControllerGenerator : BaseGenerator
    {
        internal SseHostControllerGenerator(ServiceContext dataModel)
        {
            DataModel = dataModel;

            Directory = dataModel.Config.SseHostController_Destination.Directory;
            Namespace = dataModel.Config.SseHostController_Destination.Namespace;

            Name = "SseHostController";
            FileName = $"{Name}.g.cs";
        }

        public ServiceContext DataModel { get; }

        public void GenerateCode()
        {
            Reg("Microsoft.AspNetCore.Mvc");
            Reg("Microsoft.AspNetCore.Http");
            Reg(DataModel.SseHostCollection);
            Reg(DataModel.FabricClient);
            Reg(DataModel.SseHost);
            Reg(DataModel.SseServiceId);
            Reg(DataModel.UserId);
            Reg(DataModel.SessionId);

            Code = @$"{GetNamespacesCode()}namespace {Namespace};

[ApiController]
[Route(""ssehost"")]
public class SseHostController(
    gAPI.Interfaces.IServerAuthenticationService authenticationService,
    {DataModel.SseHostCollection} sseHostCollection,
    {DataModel.FabricClient} fabricClient)
    : ControllerBase
{{
    [HttpGet(""connect/{{serviceId}}"")]
    public async Task<IResult> Connect(
        [FromRoute] string serviceId,
        [FromHeader] Guid sessionId)
    {{
        await authenticationService.InitializeAsync(sessionId);
        var userIdString = await authenticationService.GetUserId();

        var sseHost = new {DataModel.SseHost}(
            sseHostCollection,
            fabricClient,
            new {DataModel.SseServiceId}(serviceId),
            new {DataModel.UserId}(userIdString),
            new {DataModel.SessionId}(sessionId.ToString()));

        return Results.ServerSentEvents(sseHost.GetStrings());
    }}
}}";
            return;
        }
    }
}