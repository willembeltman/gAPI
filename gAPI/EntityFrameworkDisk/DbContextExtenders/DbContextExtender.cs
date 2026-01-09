using System;
using System.IO;

namespace gAPI.EntityFrameworkDisk.DbContextExtenders;

public class DbContextExtender
{
    internal DbContextExtender(
        Action<DbContext, DirectoryInfo> loadDbSetsFromDirectoryDelegate,
        string code)
    {
        LoadDbSetsFromDirectoryDelegate = loadDbSetsFromDirectoryDelegate;
        Code = code;
    }

    private readonly Action<DbContext, DirectoryInfo> LoadDbSetsFromDirectoryDelegate;

    /// <summary>
    /// The generated source code used to compile the DbContext extension logic.
    /// This is a string representation of C# code that initializes specific DbSets within the DbContext.
    /// </summary>
    public readonly string Code;

    /// <summary>
    /// Extends the given DbContext by initializing its DbSet collections with data from a directory.
    /// This is done using the dynamically compiled delegate <see cref="LoadDbSetsFromDirectoryDelegate"/>.
    /// </summary>
    /// <param name="dbContext">The DbContext to extend.</param>
    /// <param name="directory">The directory from which data is loaded.</param>
    public void LoadDbSetsFromDirectory(DbContext dbContext, DirectoryInfo directory)
    {
        LoadDbSetsFromDirectoryDelegate(dbContext, directory);
    }
}