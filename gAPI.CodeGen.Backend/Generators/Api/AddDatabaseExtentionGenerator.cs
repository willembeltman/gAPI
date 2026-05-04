using gAPI.CodeGen.Backend.Models;

namespace gAPI.CodeGen.Backend.Generators.Api;

public class AddDatabaseExtensionGenerator : BaseGenerator
{
    public AddDatabaseExtensionGenerator(
        BackendGenerator context)
    {
        Directory = context.Config.Extensions_Directory;
        Namespace = context.Config.Extensions_Namespace;

        Context = context;

        Name = "AddDatabaseExtension";
        FileName = $"{Name}.cs";
    }

    public BackendGenerator Context { get; }
    public DbContext ApplicationDbContext => Context.DbContext;

    public void GenerateCode()
    {
        Reg(ApplicationDbContext);
        Reg("Microsoft.EntityFrameworkCore");

        Code = $@"{GetNamespacesCode()}
namespace {Namespace};

public static class {Name}
{{
    public static IServiceCollection AddDatabase(this IServiceCollection services, string connectionString, bool useMemoryDatabase = false)
    {{
        // Then the database context
        if (useMemoryDatabase)
        {{
            services.AddDbContext<{ApplicationDbContext}>(options =>
                options.UseInMemoryDatabase(""InMemoryDb""));
        }}
        else
        {{
            services.AddDbContext<{ApplicationDbContext}>(options =>
            {{
                options.UseSqlServer(
                    connectionString,
                    sql => sql.EnableRetryOnFailure(
                        maxRetryCount: 10,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorNumbersToAdd: null
                    )
                );
                options.EnableSensitiveDataLogging(false);
                options.LogTo(Console.WriteLine, LogLevel.Warning);
            }});
        }}
        return services;
    }}

    public static WebApplication MapDatabase(this WebApplication app)
    {{
        //// For docker
        //try
        //{{
        //    using (var scope = app.Services.CreateScope())
        //    {{
        //        var db = scope.ServiceProvider.GetRequiredService<{ApplicationDbContext}>();
        //        await db.Database.MigrateAsync();
        //    }}
        //}}
        //catch {{ }}

        return app;
    }}
}}";
        Save(false);
    }
}
