namespace gAPI.AutoServiceInterface.Generators
{

    internal class AddAutoServiceInterfaceServicesGenerator : BaseGenerator
    {
        internal AddAutoServiceInterfaceServicesGenerator(ServiceContext serviceContext)
        {
            ServiceContext = serviceContext;

            Directory = serviceContext.Config.AddAutoServiceInterfaceServices_Destination.Directory;
            Namespace = serviceContext.Config.AddAutoServiceInterfaceServices_Destination.Namespace;

            Name = "AddAutoServiceInterfaceServicesExtention";
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
    public static void AddAutoServiceInterfaceServices(this IServiceCollection services)
    {{
{propertiesCode}    }}
}}";

        }
    }
}