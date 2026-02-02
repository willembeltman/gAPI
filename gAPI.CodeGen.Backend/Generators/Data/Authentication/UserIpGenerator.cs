using gAPI.CodeGen.Backend.Models;
using gAPI.CodeGen.Backend.Models.Entities;
using gAPI.Ids;

namespace gAPI.CodeGen.Backend.Generators.Data.Authentication;

public class UserIpGenerator : BaseGenerator
{
    public UserIpGenerator(
        BackendGenerator context)
    {
        Directory = context.Config.Data_AuthenticationDirectory;
        Namespace = context.Config.Data_AuthenticationNamespace;

        Context = context;

        Name = "UserIp";
        FileName = $"{Name}.cs";
    }

    public BackendGenerator Context { get; }

    public Entity User => Context.DbContext.UserEntity;
    public SharedReference IsHidden => Context.SharedReferences.IsHidden;
    public IpGenerator Ip => Context.Ip;
    public UserIpSessionGenerator UserIpSession => Context.UserIpSession;

    public void GenerateCode()
    {
        Reg("Microsoft.EntityFrameworkCore");
        Reg("Microsoft.EntityFrameworkCore.Metadata.Builders");
        Reg("System.ComponentModel.DataAnnotations");
        Reg("System.ComponentModel.DataAnnotations.Schema");
        Reg(IsHidden);
        Reg(User);
        Reg(Ip);
        Reg(UserIpSession);

        Code = $@"{GetNamespacesCode()}
namespace {Namespace};

[{IsHidden}]
public class {Name}
{{
    public {Name}() {{ }}
    public {Name}(
        User? user,
        Ip ip)
    {{
        User = user;
        Ip = ip;
    }}

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id {{ get; set; }}

    public {User.KeyProperty.TypeSimpleName}? UserId {{ get; set; }}
    public virtual {User}? User {{ get; set; }}

    public long IpId {{ get; set; }}
    public virtual {Ip}? Ip {{ get; set; }}

    public virtual ICollection<{UserIpSession}>? UserIpSessions {{ get; set; }}

}}

public class {Name}AddressConfiguration : IEntityTypeConfiguration<{Name}>
{{
    public void Configure(EntityTypeBuilder<{Name}> modelBuilder)
    {{
        modelBuilder
            .HasOne(cb => cb.User)
            .WithMany(cd => cd.UserIps)
            .HasForeignKey(cb => cb.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder
            .HasOne(cb => cb.Ip)
            .WithMany(cd => cd.UserIps)
            .HasForeignKey(cb => cb.IpId)
            .OnDelete(DeleteBehavior.NoAction);
    }}
}}";
        Save(false);
    }
}