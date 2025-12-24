using gAPI.AutoApi.Configs;
using gAPI.AutoApi.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.IO;
using System.Linq;
using System.Text;

namespace gAPI.AutoApi
{
    internal static class ApisGenerator
    {
        public static void Generate(ServiceContext dataModel, SourceProductionContext spc)
        {
            var Config = dataModel.Config;
            var Apis = dataModel.Services
                .Select(service => new ControllerGenerator(dataModel, service))
                .ToArray();

            foreach (var api in Apis)
            {
                api.GenerateCode();
                spc.AddSource(Path.Combine(Config.Controllers_Destination.Directory, api.FileName), SourceText.From(api.Code, Encoding.UTF8));
            }

            var AddAutoApiServices = new AddAutoApiServicesExtentionGenerator(dataModel);
            AddAutoApiServices.GenerateCode();
            spc.AddSource(Path.Combine(Config.AddAutoApiServices_Destination.Directory, AddAutoApiServices.FileName), SourceText.From(AddAutoApiServices.Code, Encoding.UTF8));

            var AddAutoApi = new AddAutoApiExtentionGenerator(dataModel);
            AddAutoApi.GenerateCode();
            spc.AddSource(Path.Combine(Config.AddAutoApiServices_Destination.Directory, AddAutoApi.FileName), SourceText.From(AddAutoApi.Code, Encoding.UTF8));
        }
    }
}