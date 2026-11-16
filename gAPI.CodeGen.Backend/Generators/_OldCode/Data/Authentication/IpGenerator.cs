//using gAPI.CodeGen.Backend.Models;

//namespace gAPI.CodeGen.Backend.Generators.Data.Authentication;

//public class IpGenerator : BaseGenerator
//{
//    public IpGenerator(
//        BackendGenerator context)
//    {
//        Directory = context.Config.Data_AuthenticationDirectory;
//        Namespace = context.Config.Data_AuthenticationNamespace;

//        Context = context;

//        Name = "Ip";
//        FileName = $"{Name}.cs";
//    }

//    public BackendGenerator Context { get; }

//    public SharedReference IsHidden => Context.SharedReferences.IsHidden;
//    public UserIpGenerator UserIp => Context.UserIp;

//    public void GenerateCode()
//    {
//        Reg("System.ComponentModel.DataAnnotations");
//        Reg(IsHidden);
//        Reg(UserIp);

//        Code = $@"{GetNamespacesCode()}
//namespace {Namespace};

//[{IsHidden}]
//public class Ip
//{{
//    public Ip() {{ }}
//    public Ip(string ipAdress)
//    {{
//        Address = ipAdress;
//    }}

//    [Key]
//    public long Id {{ get; set; }}

//    [StringLength(128)]
//    public string Address {{ get; set; }} = string.Empty;

//    public DateTimeOffset? RegisterLockedOutDate {{ get; set; }}
//    public int RegisterCount {{ get; set; }}
//    public DateTimeOffset? LoginLockedOutDate {{ get; set; }}
//    public int LoginAttempts {{ get; set; }}
//    public DateTimeOffset? ForgetPasswordLockedOutDate {{ get; set; }}
//    public int ForgetPasswordAttempts {{ get; set; }}
//    public int ChangePasswordAttempts {{ get; set; }}
//    public DateTimeOffset? ChangePasswordLockedOutDate {{ get; set; }}

//    public virtual ICollection<{UserIp}>? UserIps {{ get; set; }}
//}}";
//        Save(false);
//    }
//}