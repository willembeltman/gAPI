//using gAPI.CodeGen.Backend.Models;

//namespace gAPI.CodeGen.Backend.Generators.Data.Authentication;

//public class SessionGenerator : BaseGenerator
//{
//    public SessionGenerator(
//        BackendGenerator context)
//    {
//        Directory = context.Config.Data_AuthenticationDirectory;
//        Namespace = context.Config.Data_AuthenticationNamespace;

//        Context = context;

//        Name = "Session";
//        FileName = $"{Name}.cs";
//    }

//    public BackendGenerator Context { get; }

//    public SharedReference IsHidden => Context.SharedReferences.IsHidden;
//    public UserIpSessionGenerator UserIpSession => Context.UserIpSession;

//    public void GenerateCode()
//    {
//        Reg("System.ComponentModel.DataAnnotations");
//        Reg(IsHidden);
//        Reg(UserIpSession);

//        Code = $@"{GetNamespacesCode()}
//namespace {Namespace};

//[{IsHidden}]
//public class {Name}
//{{
//    public {Name}() {{ }}
//    public {Name}(string sessionId)
//    {{
//        SessionId = sessionId;
//    }}

//    [Key]
//    public long Id {{ get; set; }}

//    [StringLength(256)]
//    public string SessionId {{ get; set; }} = string.Empty;

//    public virtual ICollection<{UserIpSession}>? UserIpSessions {{ get; set; }}
//}}";
//        Save(false);
//    }
//}