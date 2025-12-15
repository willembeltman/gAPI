//namespace gAPI.CodeGen.Backend.Generators.Business.Extentions
//{
//    public class AddHandlersExtentionGenerator : BaseGenerator
//    {
//        public AddHandlersExtentionGenerator(BackendGenerator backendGenerator, DirectoryInfo businessExtentionsDirectory, string businessExtentionsNamespace)
//        {
//            Context = backendGenerator;

//            Directory = businessExtentionsDirectory;
//            Namespace = businessExtentionsNamespace;

//            Name = "AddHandlersExtention";
//            FileName = $"{Name}.cs";
//        }

//        public BackendGenerator Context { get; }

//        internal void GeneratorCode()
//        {
//            Reg("Microsoft.Extensions.DependencyInjection");
//            foreach (var item in Context.Dtos)
//            {
//                Reg(item.Handler);
//            }

//            Code = $@"{GetNamespacesCode()}
//namespace {Namespace}
//{{
//    public static class {Name}
//    {{
//        public static void AddHandlers(this IServiceCollection services)
//        {{{string.Join("", Context.Dtos.Select(dto => $@"
//            services.AddScoped<{dto.HandlerInterface.Name}, {dto.Handler.Name}>();"))}
//        }}
//    }}
//}}
//";
//            Save();
//        }
//    }
//}
