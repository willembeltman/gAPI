using gAPI.AutoServiceInterface.Configs;
using gAPI.AutoServiceInterface.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.IO;
using System.Linq;
using System.Text;

namespace gAPI.AutoServiceInterface
{
    internal class ServiceInterfacesGenerator
    {
        public ServiceInterfacesGenerator(ServiceContext dataModel, SourceProductionContext spc)
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

            AddAutoServiceInterfaceServices = new AddAutoServiceInterfaceServicesGenerator(dataModel);
            AddAutoServiceInterfaceServices.GenerateCode();
            var addServicesFullName = Path.Combine(Config.AddAutoServiceInterfaceServices_Destination.Directory, AddAutoServiceInterfaceServices.FileName);
            spc.AddSource(addServicesFullName, SourceText.From(AddAutoServiceInterfaceServices.Code, Encoding.UTF8));
        }

        public ServerConfig Config { get; private set; }
        public ControllerGenerator[] Apis { get; }
        public AddAutoServiceInterfaceServicesGenerator AddAutoServiceInterfaceServices { get; }
    }
}