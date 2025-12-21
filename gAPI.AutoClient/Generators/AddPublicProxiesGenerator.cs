using gAPI.AutoApiClient.Configs;

namespace gAPI.AutoApiClient.Generators
{
    internal class AddAutoClientServicesGenerator : BaseGenerator
    {
        public AddAutoClientServicesGenerator(ClientGenerator[] clients, ClientConfig config)
        {
            Clients = clients;

            Directory = config.Clients_Destination.Directory;
            Namespace = config.Clients_Destination.Namespace;

            Name = "AddAutoClientServicesExtender";
            FileName = $"{Name}.g.cs";
            Clients = clients;
        }

        public ClientGenerator[] Clients { get; }

        public void GenerateCode()
        {
            Reg("Microsoft.Extensions.DependencyInjection");

            var propertiesCode = "";
            foreach (var client in Clients)
            {
                var @interface = client.Interface;
                Reg(@interface);
                Reg(client);
                propertiesCode += $"\r\n        services.AddScoped<{@interface.Name}, {client.Name}>();";
            }

            Code = $@"{GetNamespacesCode()}namespace {Namespace};

public static class {Name}
{{
    public static void AddAutoClientServices(this IServiceCollection services)
    {{{propertiesCode}
    }}
}}";
        }
    }
}