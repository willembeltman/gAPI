//namespace gAPI.AutoSse.Generators;

//public class SseHostControllerGenerator : BaseGenerator
//{
//    public SseHostControllerGenerator(ServiceContext dataModel)
//    {
//        DataModel = dataModel;

//        Directory = dataModel.Config.SseHostController_Destination.Directory;
//        Namespace = dataModel.Config.SseHostController_Destination.Namespace;

//        Name = "SseHostController";
//        FileName = $"{Name}.g.cs";
//    }

//    public ServiceContext DataModel { get; }

//    public void GenerateCode()
//    {
//        Code = "";
//        return;

//        Reg("Microsoft.AspNetCore.Mvc");
//        Reg("Microsoft.AspNetCore.Http");
//        Reg(DataModel.IServerAuthenticationService);
//        Reg(DataModel.SseHostCollection);
//        Reg(DataModel.FabricClient);
//        Reg(DataModel.SseHost);
//        Reg(DataModel.SseServiceId);
//        Reg(DataModel.UserId);
//        Reg(DataModel.SessionId);

//        Code = $@"{GetNamespacesCode()}
//namespace {Namespace};

//[ApiController]
//[Route(""ssehost"")]
//public class SseHostController(
//    {DataModel.IServerAuthenticationService} authenticationService,
//    {DataModel.SseHostCollection} sseHostCollection,
//    {DataModel.FabricClient} fabricClient)
//    : ControllerBase
//{{
//    [HttpGet(""connect/{{serviceId}}"")]
//    public async Task<IResult> Connect(
//        [FromRoute] string serviceId,
//        [FromHeader] string sessionId)
//    {{
//        var sseHost = new {DataModel.SseHost}(
//            sseHostCollection,
//            fabricClient,
//            new {DataModel.SseServiceId}(serviceId),
//            new {DataModel.UserId}(authenticationService.UserId),
//            new {DataModel.SessionId}(authenticationService.SessionId));

//        return Results.ServerSentEvents(sseHost.ReadAllAsync());
//    }}
//}}";
//        return;
//    }
//}