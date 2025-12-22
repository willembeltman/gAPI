namespace gAPI.AutoService.Generators
{

    internal class AddAutoServiceServicesGenerator : BaseGenerator
    {
        internal AddAutoServiceServicesGenerator(ServiceContext serviceContext)
        {
            ServiceContext = serviceContext;

            Directory = serviceContext.Config.AddAutoServiceServices_Destination.Directory;
            Namespace = serviceContext.Config.AddAutoServiceServices_Destination.Namespace;

            Name = "AddAutoServiceServicesExtention";
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
    public static void AddAutoServiceServices(this IServiceCollection services)
    {{
{propertiesCode}    }}
}}";

        }
    }
}