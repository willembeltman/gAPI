using gAPI.Helpers;
using System;
using System.IO;
using System.Linq;

namespace gAPI.EntityFrameworkDisk.DbContextExtenders;


internal static class DbContextExtenderFactory
{

    /// <summary>
    /// Constructor that dynamically generates and compiles code to extend a DbContext.
    /// Based on reflection of the DbContext type, it generates C# code that initializes all DbSet properties
    /// with instances linked to either a ZipArchive or a directory.
    /// </summary>
    /// <param name="dbContext">The DbContext whose type is used for code generation.</param>
    public static DbContextExtender CreateInstance(DbContext dbContext)
    {
        var applicationDbContextType = dbContext.GetType();
        var extenderName = $"{applicationDbContextType.Name}DbContextFactory";
        var extenderMethodNameDirectory = "ExtendDbContextDirectory";

        // Genereer de C# broncode die de extend-methodes definieert
        var Code = GenerateDbSetLoaderCode(applicationDbContextType, extenderName, extenderMethodNameDirectory);

        // Compileer de gegenereerde code in een assembly
        var asm = CodeCompiler.Compile(Code);

        // Haal het type van de extender class uit de assembly
        var serializerType = asm.GetType(extenderName);

        // Haal de methodes op die de DbContext extensies uitvoeren
        var LoadDbSetsFromDirectoryMethod = serializerType!.GetMethod(extenderMethodNameDirectory);

        // Maak delegates aan voor de methodes zodat ze snel aangeroepen kunnen worden
        var LoadDbSetsFromDirectoryDelegate = (Action<DbContext, DirectoryInfo>)Delegate.CreateDelegate(
             typeof(Action<DbContext, DirectoryInfo>), LoadDbSetsFromDirectoryMethod!);

        return new DbContextExtender(LoadDbSetsFromDirectoryDelegate, Code);
    }

    /// <summary>
    /// Generates the source code for an extender class that contains two methods:
    /// - ExtendDbContextZip: initializes DbSets using a ZipArchive
    /// - ExtendDbContextDirectory: initializes DbSets using a DirectoryInfo
    /// 
    /// For each public, settable DbSet property in the DbContext, code is generated that creates a new
    /// DbSet instance backed by either the zip archive or the directory.
    /// </summary>
    /// <param name="applicationDbContextType">The type of the DbContext.</param>
    /// <param name="extenderName">The name of the generated extender class.</param>
    /// <param name="extenderZipMethodName">The name of the method that uses a ZipArchive.</param>
    /// <param name="extenderDirectoryMethodName">The name of the method that uses a DirectoryInfo.</param>
    /// <returns>The complete generated source code as a string.</returns>
    private static string GenerateDbSetLoaderCode(Type applicationDbContextType, string extenderName, string extenderDirectoryMethodName)
    {
        var applicationDbContextName = applicationDbContextType.Name;
        var applicationDbContextFullName = applicationDbContextType.FullName;

        var dbContextType = typeof(DbContext);
        var dbContextTypeFullName = dbContextType.FullName;

        var dbSetType = typeof(DbSet<>);
        var dbSetFullName = dbSetType.FullName!.Split('`').First();

        var propertiesDirecoryCode = string.Empty;
        var props = applicationDbContextType.GetProperties();
        foreach (var property in props)
        {
            if (!ReflectionHelper.HasPublicGetter(property)) continue;
            if (!ReflectionHelper.HasPublicSetter(property)) continue;
            if (!ReflectionHelper.IsDbSet(property)) continue;

            var propertyName = property.Name;

            var propertyType = ReflectionHelper.GetDbSetType(property);
            var propertyTypeName = propertyType.Name;
            var propertyTypeFullName = propertyType.FullName;
            propertiesDirecoryCode += $@"
                    db.{propertyName} = new {dbSetFullName}<{propertyTypeFullName}>(db, directory);";
        }

        return $@"
            using System;

            public static class {extenderName}
            {{
                public static void {extenderDirectoryMethodName}({dbContextTypeFullName} dbContext, System.IO.DirectoryInfo directory)
                {{
                    var db = dbContext as {applicationDbContextFullName};
                    if (db == null) throw new Exception(""dbContext is not of type {applicationDbContextName}"");
                    {propertiesDirecoryCode}
                }}
            }}";
    }
}