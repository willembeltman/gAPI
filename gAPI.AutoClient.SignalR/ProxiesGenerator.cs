using gAPI.AutoClient.SignalR.Configs;
using gAPI.AutoClient.SignalR.Contexts;
using gAPI.AutoClient.SignalR.Generators;
using Microsoft.CodeAnalysis.Text;
using System.IO;
using System.Linq;
using System.Text;

namespace gAPI.AutoClient.SignalR
{
    internal class ClientsGenerator
    {
        public ClientsGenerator(ServiceContext dataModel, Microsoft.CodeAnalysis.SourceProductionContext spc)
        {
            ServiceContext = dataModel;
            Config = dataModel.Config;

            FormFile = new FormFileGenerator(Config);
            FormFile.GenerateCode();
            var formFileFullName = Path.Combine(Config.Helpers_Destination.Directory, FormFile.FileName);
            spc.AddSource(formFileFullName, SourceText.From(FormFile.Code, Encoding.UTF8));

            ToFormFileAsyncExtention = new ToFormFileAsyncExtentionGenerator(Config);
            ToFormFileAsyncExtention.GenerateCode();
            var toFormFileExtentionFullName = Path.Combine(Config.Helpers_Destination.Directory, ToFormFileAsyncExtention.FileName);
            spc.AddSource(toFormFileExtentionFullName, SourceText.From(ToFormFileAsyncExtention.Code, Encoding.UTF8));

            //ItemDataSource = new ItemDataSourceGenerator(Config);
            //ItemDataSource.GenerateCode();
            //var itemDataSourceFullName = Path.Combine(Config.Helpers_Destination.Directory, ItemDataSource.FileName);
            //spc.AddSource(itemDataSourceFullName, SourceText.From(ItemDataSource.Code, Encoding.UTF8));

            //ListDataSource = new ListDataSourceGenerator(Config);
            //ListDataSource.GenerateCode();
            //var listDataSourceFullName = Path.Combine(Config.Helpers_Destination.Directory, ListDataSource.FileName);
            //spc.AddSource(listDataSourceFullName, SourceText.From(ListDataSource.Code, Encoding.UTF8));

            Clients = ServiceContext.Interfaces
                .Select(service => new ClientGenerator(service, Config))
                .ToArray();

            foreach (var client in Clients)
            {
                client.GenerateCode();
                var clientFullName = Path.Combine(Config.Clients_Destination.Directory, client.FileName);
                spc.AddSource(clientFullName, SourceText.From(client.Code, Encoding.UTF8));
            }

            AddAutoClientServices = new AddAutoClientServicesGenerator(Clients, Config);
            AddAutoClientServices.GenerateCode();
            var addAutoClientServicesFullName = Path.Combine(Config.Clients_Destination.Directory, AddAutoClientServices.FileName);
            spc.AddSource(addAutoClientServicesFullName, SourceText.From(AddAutoClientServices.Code, Encoding.UTF8));
        }

        public ServiceContext ServiceContext { get; }
        public ClientConfig Config { get; }
        public ClientGenerator[] Clients { get; }
        public AddAutoClientServicesGenerator AddAutoClientServices { get; }
        public FormFileGenerator FormFile { get; }
        public ToFormFileAsyncExtentionGenerator ToFormFileAsyncExtention { get; }
        //public ItemDataSourceGenerator ItemDataSource { get; }
        //public ListDataSourceGenerator ListDataSource { get; }
    }
}