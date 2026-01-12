using System.IO;

namespace gAPI.Helpers;

public class EnvironmentPathHelper
{
    public static DirectoryInfo GetDirectory(string root, string path)
    {
        var fullName = Path.Combine(
            root.Replace('\\', Path.DirectorySeparatorChar)
                .Replace('/', Path.DirectorySeparatorChar),
            path.Replace('\\', Path.DirectorySeparatorChar)
                .Replace('/', Path.DirectorySeparatorChar));
        return new DirectoryInfo(fullName);
    }
    public static string GetRoot(string EnvironmentProcessPath, string generatorProjectName)
    {
        var dir = new DirectoryInfo(EnvironmentProcessPath);
        var up = 0;
        while (dir!.Name != generatorProjectName)
        {
            up++;
            dir = dir.Parent;
        }

        var root = "";
        for (var i = 0; i < up - 1; i++)
        {
            root += @"..\";
        }
        return root;
    }
}
