using gAPI.AutoApi.SignalR.Configs;
using gAPI.AutoApi.SignalR.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.IO;
using System.Linq;
using System.Text;

namespace gAPI.AutoApi.SignalR
{
    internal class ApisGenerator
    {
        public ApisGenerator(ServiceContext dataModel, SourceProductionContext spc)
        {
            Config = dataModel.Config;
            Apis = dataModel.Services
                .Select(service => new ControllerGenerator(dataModel, service))
                .ToArray();

            foreach (var api in Apis)
            {
                api.GenerateCode();
                var apiFullName = Path.Combine(Config.Controllers_Destination.Directory, api.FileName);
                spc.AddSource(apiFullName, SourceText.From(api.Code, Encoding.UTF8));
            }

            AddAutoApiServices = new AddAutoApiServicesGenerator(dataModel);
            AddAutoApiServices.GenerateCode();
            var addServicesFullName = Path.Combine(Config.AddAutoApiServices_Destination.Directory, AddAutoApiServices.FileName);
            spc.AddSource(addServicesFullName, SourceText.From(AddAutoApiServices.Code, Encoding.UTF8));
        }

        public ServerConfig Config { get; private set; }
        public ControllerGenerator[] Apis { get; }
        public AddAutoApiServicesGenerator AddAutoApiServices { get; }
    }
}