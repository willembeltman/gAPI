//using gAPI.Core.Dtos;
//using gAPI.Core.Ids;
//using gAPI.Core.Interfaces;
//using gAPI.Core.Server.Enums;
//using gAPI.Core.Wss;
//using Microsoft.Extensions.Logging;

//namespace gAPI.Core.Server.Fabric;

//public class FabricClientReceiver(
//    FabricClient fabricClient,
//    ILoggerFactory loggerFactory)
//{
//    readonly ILogger Logger = loggerFactory.CreateLogger<FabricClientReceiver>();

//    public FabricConnectionId? FabricHostId { get => fabricClient.Id; private set => fabricClient.Id = value; }


//    //private async Task Receive_Log_FromFabricAsync(WssLoggerLogDto log, CancellationToken ct)
//    //{
//    //    if (log.Category == null)
//    //        return;
//    //    var logger = loggerFactory.CreateLogger(log.Category);
//    //    logger.Log(
//    //        log.Level,
//    //        log.Message,
//    //        log.Data?
//    //            .Select(a => new KeyValuePair<string, string?>(a.Key, a.Value))
//    //            .ToArray()
//    //    );
//    //}
//}
