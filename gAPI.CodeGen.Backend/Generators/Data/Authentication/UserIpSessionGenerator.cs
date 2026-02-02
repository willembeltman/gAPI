using gAPI.CodeGen.Backend.Models;

namespace gAPI.CodeGen.Backend.Generators.Data.Authentication;

public class UserIpSessionGenerator : BaseGenerator
{
    public UserIpSessionGenerator(
        BackendGenerator context)
    {
        Directory = context.Config.Data_AuthenticationDirectory;
        Namespace = context.Config.Data_AuthenticationNamespace;

        Context = context;

        Name = "UserIpSession";
        FileName = $"{Name}.cs";
    }

    public BackendGenerator Context { get; }

    public SharedReference IsHidden => Context.SharedReferences.IsHidden;
    public UserIpGenerator UserIp => Context.UserIp;
    public SessionGenerator Session => Context.Session;
    public UserIpSessionTokenGenerator UserIpSessionToken => Context.UserIpSessionToken;

    public void GenerateCode()
    {
        Reg("Microsoft.EntityFrameworkCore");
        Reg("Microsoft.EntityFrameworkCore.Metadata.Builders");
        Reg("System.ComponentModel.DataAnnotations");
        Reg("System.ComponentModel.DataAnnotations.Schema");
        Reg(IsHidden);
        Reg(UserIp);
        Reg(Session);
        Reg(UserIpSessionToken);

        Code = $@"{GetNamespacesCode()}
namespace {Namespace};

[{IsHidden}]
public class {Name}
{{
    public {Name}() {{ }}
    public {Name}(
        UserIp userIp,
        Session session)
    {{
        UserIp = userIp;
        Session = session;
    }}

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id {{ get; set; }}

    public long UserIpId {{ get; set; }}
    public virtual {UserIp}? UserIp {{ get; set; }}

    public long SessionId {{ get; set; }}
    public virtual {Session}? Session {{ get; set; }}

    public virtual ICollection<{UserIpSessionToken}>? UserIpSessionTokens {{ get; set; }}

}}

public class {Name}Configuration : IEntityTypeConfiguration<{Name}>
{{
    public void Configure(EntityTypeBuilder<{Name}> modelBuilder)
    {{
        modelBuilder
            .HasOne(cb => cb.UserIp)
            .WithMany(cd => cd.UserIpSessions)
            .HasForeignKey(cb => cb.UserIpId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder
            .HasOne(cb => cb.Session)
            .WithMany(cd => cd.UserIpSessions)
            .HasForeignKey(cb => cb.SessionId)
            .OnDelete(DeleteBehavior.NoAction);
    }}
}}";
        Save(false);
    }
}