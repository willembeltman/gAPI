//namespace gAPI.AutoApi.SignalR.Generators
//{

//    internal class AddAutoApiServicesGenerator : BaseGenerator
//    {
//        internal AddAutoApiServicesGenerator(ServiceContext serviceContext)
//        {
//            ServiceContext = serviceContext;

//            Directory = serviceContext.Config.AddAutoApiServices_Destination.Directory;
//            Namespace = serviceContext.Config.AddAutoApiServices_Destination.Namespace;

//            Name = "AddAutoApiSignalRServicesExtention";
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
//    public static void AddAutoApiSignalRServices(this IServiceCollection services)
//    {{
//{propertiesCode}    }}
//}}";

//        }
//    }
//}