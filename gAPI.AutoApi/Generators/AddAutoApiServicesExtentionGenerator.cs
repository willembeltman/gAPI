namespace gAPI.AutoApi.Generators
{

    internal class AddAutoApiServicesExtentionGenerator : BaseGenerator
    {
        internal AddAutoApiServicesExtentionGenerator(ServiceContext serviceContext)
        {
            ServiceContext = serviceContext;

            Directory = serviceContext.Config.AddAutoApiServices_Destination.Directory;
            Namespace = serviceContext.Config.AddAutoApiServices_Destination.Namespace;

            Name = "AddAutoApiServicesExtention";
            FileName = $"{Name}.g.cs";
        }

        public ServiceContext ServiceContext { get; }

        internal void GenerateCode()
        {
            Reg("Microsoft.Extensions.DependencyInjection");
            var propertiesCode = "";
            foreach (var service in ServiceContext.Services)
            {
                Reg(service.Namespace);
                Reg(service.Interface);
                propertiesCode += $"        services.AddScoped<{service.Interface.Name}, {service.Name}>();\r\n";
            }

            Code = $@"{GetNamespacesCode()}namespace {Namespace};

public static class {Name}
{{
    public static void AddAutoApiServices(this IServiceCollection services)
    {{
{propertiesCode}    }}
}}";

        }
    }
}