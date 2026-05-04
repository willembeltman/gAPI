//using gAPI.CodeGen.Backend.Models;
//using gAPI.CodeGen.Backend.Models.Entities;

//namespace gAPI.CodeGen.Backend.Generators.Data.Authentication;

//public class TokenGenerator : BaseGenerator
//{
//    public TokenGenerator(
//        BackendGenerator context)
//    {
//        Directory = context.Config.Data_AuthenticationDirectory;
//        Namespace = context.Config.Data_AuthenticationNamespace;

//        Context = context;

//        Name = "Token";
//        FileName = $"{Name}.cs";
//    }

//    public BackendGenerator Context { get; }

//    public UserIpSessionTokenGenerator UserIpSessionToken => Context.UserIpSessionToken;
//    public SharedReference IsHidden => Context.SharedReferences.IsHidden;
//    public Entity User => Context.DbContext.UserEntity;

//    public void GenerateCode()
//    {
//        Reg("System.ComponentModel.DataAnnotations");
//        Reg("Microsoft.EntityFrameworkCore");
//        Reg("Microsoft.EntityFrameworkCore.Metadata.Builders");
//        Reg(IsHidden); 
//        Reg(User);
//        Reg(UserIpSessionToken);

//        Code = $@"{GetNamespacesCode()}
//namespace {Namespace};

//[IsHidden]
//public class {Name}
//{{
//    public {Name}() {{ }}
//    public {Name}(User user, string tokenHash)
//    {{
//        User = user;
//        TokenHash = tokenHash;
//    }}

//    [Key]
//    public long Id {{ get; set; }}

//    public {User.KeyProperty.TypeSimpleName} UserId {{ get; set; }}
//    public virtual {User}? User {{ get; set; }}

//    [StringLength(280)]
//    public string TokenHash {{ get; set; }} = string.Empty;
//    public DateTime Date {{ get; set; }} = DateTime.Now;

//    public virtual ICollection<{UserIpSessionToken}>? UserIpSessionTokens {{ get; set; }}

//}}

//public class {Name}Configuration : IEntityTypeConfiguration<{Name}>
//{{
//    public void Configure(EntityTypeBuilder<{Name}> modelBuilder)
//    {{
//        modelBuilder
//            .HasOne(cb => cb.User)
//            .WithMany(u => u.Tokens)
//            .HasForeignKey(cb => cb.UserId)
//            .OnDelete(DeleteBehavior.NoAction);
//    }}
//}}";
//        Save(false);
//    }
//}