using gAPI.AutoService.Configs;
using gAPI.AutoService.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.IO;
using System.Linq;
using System.Text;

namespace gAPI.AutoService
{
    internal class ServicesGenerator
    {
        public ServicesGenerator(ServiceContext dataModel, SourceProductionContext spc)
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

            AddAutoServiceServices = new AddAutoServiceServicesGenerator(dataModel);
            AddAutoServiceServices.GenerateCode();
            var addServicesFullName = Path.Combine(Config.AddAutoServiceServices_Destination.Directory, AddAutoServiceServices.FileName);
            spc.AddSource(addServicesFullName, SourceText.From(AddAutoServiceServices.Code, Encoding.UTF8));
        }

        public ServerConfig Config { get; private set; }
        public ControllerGenerator[] Apis { get; }
        public AddAutoServiceServicesGenerator AddAutoServiceServices { get; }
    }
}