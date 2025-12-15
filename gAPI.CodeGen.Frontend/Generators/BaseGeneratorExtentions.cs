using gAPI.AutoComponents.Interfaces;

namespace gAPI.CodeGen.Frontend.Generators;


public static class BaseGeneratorExtentions
{
    public static void Save(this IBaseGenerator baseGenerator, bool overwrite = true)
    {
        if (baseGenerator.Directory == null || string.IsNullOrWhiteSpace(baseGenerator.FileName) || baseGenerator.Code == null)
        {
            throw new InvalidOperationException("Directory, Name, and Code must be set before saving.");
        }
        var filePath = Path.Combine(baseGenerator.Directory, baseGenerator.FileName);

        Console.WriteLine(filePath);

        var fileInfo = new FileInfo(filePath);
        if (overwrite || !fileInfo.Exists)
        {
            if (!fileInfo.Directory!.Exists)
            {
                fileInfo.Directory.Create();
            }
            File.WriteAllText(filePath, baseGenerator.Code);
        }
    }
}
