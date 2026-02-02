using gAPI.CodeGen.Backend.Models;

namespace gAPI.CodeGen.Backend.Generators.Data.Authentication;

public class UserIpSessionTokenRouteGenerator : BaseGenerator
{
    public UserIpSessionTokenRouteGenerator(
        BackendGenerator context)
    {
        Directory = context.Config.Data_AuthenticationDirectory;
        Namespace = context.Config.Data_AuthenticationNamespace;

        Context = context;

        Name = "UserIpSessionTokenRoute";
        FileName = $"{Name}.cs";
    }

    public BackendGenerator Context { get; }

    public SharedReference IsHidden => Context.SharedReferences.IsHidden;
    public UserIpSessionTokenGenerator UserIpSessionToken => Context.UserIpSessionToken;
    public RouteGenerator Route => Context.Route;
    public UserIpSessionTokenRouteRequestGenerator UserIpSessionTokenRouteRequest => Context.UserIpSessionTokenRouteRequest;

    public void GenerateCode()
    {
        Reg("Microsoft.EntityFrameworkCore");
        Reg("Microsoft.EntityFrameworkCore.Metadata.Builders");
        Reg("System.ComponentModel.DataAnnotations");
        Reg("System.ComponentModel.DataAnnotations.Schema");
        Reg(IsHidden);
        Reg(UserIpSessionToken);
        Reg(Route);
        Reg(UserIpSessionTokenRouteRequest);

        Code = $@"{GetNamespacesCode()}
namespace {Namespace};

[{IsHidden}]
public class {Name}
{{
    public {Name}() {{ }}
    public {Name}(
        {UserIpSessionToken}? userIpSessionCompanyToken,
        {Route}? route)
    {{
        UserIpSessionToken = userIpSessionCompanyToken;
        Route = route;
    }}

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id {{ get; set; }}

    public long UserIpSessionTokenId {{ get; set; }}
    public virtual {UserIpSessionToken}? UserIpSessionToken {{ get; set; }}

    public long RouteId {{ get; set; }}
    public virtual {Route}? Route {{ get; set; }}

    public virtual ICollection<{UserIpSessionTokenRouteRequest}>? UserIpSessionTokenRouteRequests {{ get; set; }}
}}

public class {Name}Configuration : IEntityTypeConfiguration<{Name}>
{{
    public void Configure(EntityTypeBuilder<{Name}> modelBuilder)
    {{
        modelBuilder
            .HasOne(cb => cb.UserIpSessionToken)
            .WithMany(cd => cd.UserIpSessionTokenRoutes)
            .HasForeignKey(cb => cb.UserIpSessionTokenId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder
            .HasOne(cb => cb.Route)
            .WithMany(cd => cd.UserIpSessionTokenRoutes)
            .HasForeignKey(cb => cb.RouteId)
            .OnDelete(DeleteBehavior.NoAction);
    }}
}}";
        Save(false);
    }
}