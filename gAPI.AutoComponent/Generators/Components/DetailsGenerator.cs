using gAPI.AutoComponent.Interfaces;
using System.Linq;

namespace gAPI.AutoComponent.Generators.Components;

public class DetailsGenerator : BaseGenerator
{
    public DetailsGenerator(
        ICrudlType dto,
        ISharedReference itemDataSource,
        IBaseGenerator imports,
        string directory,
        string @namespace)
    {
        Dto = dto;
        ItemDataSource = itemDataSource;
        Imports = imports;

        Directory = directory;
        Namespace = @namespace;

        Name = $"{dto.Name}Details";
        FileName = $"{Name}.razor";
    }

    public ICrudlType Dto { get; }
    public ISharedReference ItemDataSource { get; }
    public IBaseGenerator Imports { get; }

    public void GenerateCode()
    {
        // Registreer namespaces
        Imports.Reg(Dto);
        Imports.Reg(ItemDataSource);
        Imports.Reg("Microsoft.AspNetCore.Components");

        if (Dto.IsStorageFileUrlProperty)
            Imports.Reg("Microsoft.AspNetCore.Components.Forms");

        if (Dto.Name == null)
            return;

        var properties = Dto.Properties.Where(a => a.ListByMethod == null);
        if (Dto.IsICrudEntity)
        {
            properties = properties.Where(a => a.Name != "CanUpdate" && a.Name != "CanDelete");
        }

        var propertyMarkup = string.Join("\r\n", properties.Select(a => GetPropertyMarkup(a)));
        var storageMarkup = Dto.IsStorageFileUrlProperty ? GetStorageFileView() : "";

        // Volledige Razor view
        Code = GetRazorNamespacesCode() + $@"@if (DataSource?.Model == null)
{{
    <text><!-- niets te tonen --></text>
}}
else
{{    
    {storageMarkup}
    {propertyMarkup}
}}

@code {{
    [Parameter, EditorRequired]
    public ItemDataSource<{Dto.Name}, {Dto.KeyProperty.TypeSimpleName}>? DataSource {{ get; set; }}
}}";
    }

    private string GetPropertyMarkup(ICrudlProperty p)
    {
        // Als het nullable is, voeg ? toe
        var nullSafe = p.PropertyType.IsNullable ? "?" : "";
        return $@"
    <div class=""mb-2"">
        <strong>{p.Name}</strong>: @DataSource.Model.{p.Name}{nullSafe}.ToString()
    </div>";
    }

    private string GetStorageFileView()
    {
        // Houdt rekening met DataSource.Model!
        return $@"
    @if (!string.IsNullOrWhiteSpace(DataSource.Model!.StorageFileUrl))
    {{
        <div class=""mb-3"">
            @if (new[] {{ ""png"", ""jpg"", ""jpeg"", ""gif"", ""bmp"", ""webp"" }}.
                Contains(System.IO.Path.GetExtension(DataSource.Model!.StorageFileUrl)!.TrimStart('.').ToLowerInvariant()))
            {{
                <img src=""@DataSource.Model!.StorageFileUrl"" style=""max-height: 120px;"" />
            }}
            else
            {{
                <a href=""@DataSource.Model!.StorageFileUrl"" download>Download</a>
            }}
        </div>
    }}";
    }
}
