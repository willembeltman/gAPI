//using gAPI.CodeGen.Backend.Models;

//namespace gAPI.CodeGen.Backend.Generators.Data.Authentication;

//public class RouteGenerator : BaseGenerator
//{
//    public RouteGenerator(
//        BackendGenerator context)
//    {
//        Directory = context.Config.Data_AuthenticationDirectory;
//        Namespace = context.Config.Data_AuthenticationNamespace;

//        Context = context;

//        Name = "Route";
//        FileName = $"{Name}.cs";
//    }

//    public BackendGenerator Context { get; }

//    public SharedReference IsHidden => Context.SharedReferences.IsHidden;
//    public UserIpSessionTokenRouteGenerator UserIpSessionTokenRoute => Context.UserIpSessionTokenRoute;

//    public void GenerateCode()
//    {
//        Reg("System.ComponentModel.DataAnnotations");
//        Reg("System.ComponentModel.DataAnnotations.Schema");
//        Reg(IsHidden);
//        Reg(UserIpSessionTokenRoute);

//        Code = $@"{GetNamespacesCode()}
//namespace {Namespace};

//[{IsHidden}]
//public class {Name}
//{{
//    public {Name}() {{ }}
//    public {Name}(string route)
//    {{
//        RouteName = route;
//    }}

//    [Key]
//    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
//    public long Id {{ get; set; }}

//    public string? RouteName {{ get; set; }} = string.Empty;

//    public virtual ICollection<{UserIpSessionTokenRoute}>? UserIpSessionTokenRoutes {{ get; set; }}
//}}";
//        Save(false);
//    }
//}