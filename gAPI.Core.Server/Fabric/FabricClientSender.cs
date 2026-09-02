using gAPI.Core.Dtos;
using gAPI.Core.Ids;
using gAPI.Core.Server.Enums;
using gAPI.Core.Server.Interfaces;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;

namespace gAPI.Core.Server.Fabric;

public class FabricClientSender(
    FabricClient fabricClient,
    ILoggerFactory loggerFactory)
{
    private readonly ILogger Logger = loggerFactory.CreateLogger<FabricClientSender>();

}