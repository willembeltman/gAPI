using gAPI.AutoHubClient.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.IO;
using System.Text;

namespace gAPI.AutoHubClient
{
    internal class HubClientsGenerator
    {
        internal static void Generate(ServiceContext dataModel, SourceProductionContext spc)
        {
            var Config = dataModel.Config;

            var ISignalRConnection = new ISignalRConnectionGenerator(dataModel);
            ISignalRConnection.GenerateCode();
            var signalRHubFullName = Path.Combine(Config.HubClients_Destination.Directory, ISignalRConnection.FileName);
            spc.AddSource(signalRHubFullName, SourceText.From(ISignalRConnection.Code, Encoding.UTF8));

            var SignalRConnection = new SignalRConnectionGenerator(dataModel, ISignalRConnection);
            SignalRConnection.GenerateCode();
            var signalRContextFullName = Path.Combine(Config.HubClients_Destination.Directory, SignalRConnection.FileName);
            spc.AddSource(signalRContextFullName, SourceText.From(SignalRConnection.Code, Encoding.UTF8));

            //AddAutoClientServices = new AddAutoClientServicesGenerator(dataModel);
            //AddAutoClientServices.GenerateCode();
            //var addServicesFullName = Path.Combine(Config.AddAutoClientServices_Destination.Directory, AddAutoClientServices.FileName);
            //spc.AddSource(addServicesFullName, SourceText.From(AddAutoClientServices.Code, Encoding.UTF8));
        }
    }
}