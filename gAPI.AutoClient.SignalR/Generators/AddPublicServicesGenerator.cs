//namespace gAPI.AutoHubClient.Generators
//{

//    internal class AddAutoClientServicesGenerator : BaseGenerator
//    {
//        internal AddAutoClientServicesGenerator(ServiceContext serviceContext)
//        {
//            ServiceContext = serviceContext;

//            Directory = serviceContext.Config.AddAutoClientServices_Destination.Directory;
//            Namespace = serviceContext.Config.AddAutoClientServices_Destination.Namespace;

//            Name = "AddAutoClientSignalRServicesExtention";
//            FileName = $"{Name}.g.cs";
//        }

//        public ServiceContext ServiceContext { get; }

//        internal void GenerateCode()
//        {
//            Reg("Microsoft.Extensions.DependencyInjection");
//            var propertiesCode = "";
//            foreach (var service in ServiceContext.ClientHandlers)
//            {
//                Reg(service.Namespace);
//                Reg(service.Interface);
//                propertiesCode += $"        services.AddScoped<{service.Interface.Name}, {service.Name}>();\r\n";
//            }

//            Code = $@"{GetNamespacesCode()}namespace {Namespace};

//public static class {Name}
//{{
//    public static void AddAutoClientSignalRServices(this IServiceCollection services)
//    {{
//{propertiesCode}    }}
//}}";

//        }
//    }
//}