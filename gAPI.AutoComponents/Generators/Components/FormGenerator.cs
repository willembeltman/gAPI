using gAPI.AutoComponents.Helpers;
using gAPI.AutoComponents.Interfaces;
using System.Linq;

namespace gAPI.AutoComponents.Generators.Components;

public class FormGenerator : BaseGenerator
{
    public FormGenerator(
        ICrudlType dto,
        ISharedReference itemDataSource,
        ISharedReference listDataSource,
        ISharedReference iClientAuthenticationService,
        ISharedReference formFile,
        ISharedReference toFormFileAsyncExtention,
        IBaseGenerator imports,
        string directory,
        string @namespace) : base(directory, @namespace)
    {
        CrudlType = dto;
        ItemDataSource = itemDataSource;
        ListDataSource = listDataSource;
        Imports = imports;
        IClientAuthenticationService = iClientAuthenticationService;
        FormFile = formFile;
        ToFormFileAsyncExtention = toFormFileAsyncExtention;

        Name = $"{dto.Name}Form";
        FileName = $"{Name}.razor";
    }

    public IBaseGenerator Imports { get; }
    public ISharedReference IClientAuthenticationService { get; }
    public ISharedReference FormFile { get; }
    public ISharedReference ToFormFileAsyncExtention { get; }
    public ICrudlType CrudlType { get; }
    public ISharedReference ItemDataSource { get; }
    public ISharedReference ListDataSource { get; }

    public void GenerateCode()
    {
        Imports.RegRange(
        [
            "Microsoft.AspNetCore.Components",
            "Microsoft.AspNetCore.Components.Forms",
            "Microsoft.AspNetCore.Components.Web",
            "System",
            "System.Collections.Generic",
            "System.Threading.Tasks"
        ]);
        Imports.Reg(CrudlType);
        Imports.Reg(ItemDataSource);
        Imports.Reg(ListDataSource);
        Imports.Reg(IClientAuthenticationService);

        if (CrudlType.IsStorageFile)
        {
            Imports.Reg("Microsoft.AspNetCore.Http");
            Imports.Reg(FormFile);
            Imports.Reg(ToFormFileAsyncExtention);
        }

        var clients = CrudlType.ForeignItemProperties
            .Where(p => !p.IsStateManaged || CrudlType.IsUser)
            .ToArray();
        foreach (var client in clients)
        {
            Imports.Reg(client.ForeignKeyType);
            Imports.Reg(client.ListMethod?.Client);
        }

        foreach (var p in CrudlType.Properties)
        {
            Imports.Reg(p.TypeDigger);
            Imports.Reg(p.ForeignKeyType);
        }

        var storageFileMarkup = CrudlType.IsStorageFile ? GetStorageFileView() : "";

        var properties = CrudlType.Properties
            .Where(p =>
                (!p.IsStateManaged || CrudlType.IsUser) &&
                !p.IsReadOnly &&
                !p.IsForeignName &&
                !p.IsKey)
            .ToArray();

        var propertySections = string.Join("\n", properties.Select(a => GetPropertyMarkup(a)));

        // Foreign dropdown parameters
        var foreignParameters = string.Join("\n", clients.Select(p =>
$@"    [Parameter, EditorRequired]
    public {ListDataSource.Name}<{p.ForeignKeyType!.Name}, {p.ForeignKeyType!.KeyProperty.TypeSimpleName}>? {p.ForeignKeyType!.Name.ToMultiple()} {{ get; set; }}"));

        // Razor file output
        Code = $@"@if (DataSource?.Model == null)
{{
    <text><!-- niets te renderen --></text>
}}
else
{{
    {storageFileMarkup}
    {propertySections}
}}

@code {{
    [Parameter, EditorRequired]
    public {ItemDataSource.Name}<{CrudlType.Name}, {CrudlType.KeyProperty.TypeSimpleName}>? DataSource {{ get; set; }}

{foreignParameters}
}}";
    }

    private string GetPropertyMarkup(ICrudlProperty p)
    {
        string model = "DataSource.Model";
        string id = p.Name.ToCamelCase();

        // Foreign key dropdown
        if (p.ForeignKeyType != null && p.ForeignKeyNameProperty != null)
        {
            var dsName = p.ForeignKeyType.Name.ToMultiple();
            //string nullablePrefix = p.PropertyType.IsNullable ? "Nullable" : "";
            string bindAttr = p.PropertyType.IsNullable ? "bind-NullableValue" : "bind-Value";
            string bindTypeAttr = p.PropertyType.IsNullable ? "bindtype_NullableValue" : "bindtype_Value";
            string valueType = p.TypeSimpleName;

            return $@"
    <div class=""mb-3"">
        <label for=""{id}"" class=""form-label"">{p.ForeignKeyType.Name}</label>
        <{p.ForeignKeyType.Name}DropDown @{bindAttr}=""{model}.{p.Name}"" {bindTypeAttr}=""{valueType}""
            @bind-ForeignName=""{model}.{p.ForeignKeyNameProperty.Name}"" bindtype_ForeignName=""string""
            DataSource=""{dsName}"" id=""{id}"" />
    </div>";
        }

        if (p.IsNumber)
        {
            return $@"
    <div class=""mb-3"">
        <label for=""{id}"" class=""form-label"">{p.Name}</label>
        <InputNumber @bind-Value=""{model}.{p.Name}"" bindtype_Value=""{p.TypeSimpleName}""
            id=""{id}"" class=""form-control"" />
    </div>";
        }

        if (p.IsDateTime)
        {
            return $@"
    <div class=""mb-3"">
        <label for=""{id}"" class=""form-label"">{p.Name}</label>
        <InputDate @bind-Value=""{model}.{p.Name}"" bindtype_Value=""{p.TypeSimpleName}""
            id=""{id}"" class=""form-control"" />
    </div>";
        }

        if (p.IsCheckbox)
        {
            return $@"
    <div class=""mb-3"">
        <label for=""{id}"" class=""form-label"">{p.Name}</label>
        <InputCheckbox @bind-Value=""{model}.{p.Name}"" bindtype_Value=""{p.TypeSimpleName}""
            id=""{id}"" class=""form-check-input"" />
    </div>";
        }

        if (p.IsEnum)
        {
            return $@"
    <div class=""mb-3"">
        <label for=""{id}"" class=""form-label"">{p.Name}</label>
        <InputSelect @bind-Value=""{model}.{p.Name}"" bindtype_Value=""{p.TypeSimpleName}""
            id=""{id}"" class=""form-select"">
            @foreach (var value in Enum.GetValues(typeof({p.TypeDigger.FullName})).Cast<{p.TypeDigger.FullName}>())
            {{
                <option value=""@(value)"">@(value.ToString())</option>
            }}
        </InputSelect>
    </div>";
        }

        // Default: InputText
        return $@"
    <div class=""mb-3"">
        <label for=""{id}"" class=""form-label"">{p.Name}</label>
        <InputText @bind-Value=""{model}.{p.Name}"" bindtype_Value=""{p.TypeSimpleName}""
            id=""{id}"" class=""form-control"" />
    </div>";
    }

    private string GetStorageFileView()
    {
        return $@"
    @if (!string.IsNullOrWhiteSpace(DataSource.Model.StorageFileUrl))
    {{
        <div class=""mb-3 storageFilePreview"">
            <img src=""@(DataSource.Model.StorageFileUrl)"" style=""max-height: 120px;"" />
            <button type=""button"" class=""btn btn-sm btn-link text-danger"" @onclick=""DataSource.HandleFileRemoved"">❌ Remove file</button>
        </div>
    }}

    <div class=""mb-3 storageFile"">
        <label for=""file"" class=""form-label"">Upload file</label>
        <InputFile OnChange=""DataSource.HandleFileSelected"" key=""@(DataSource.FileInputKey)"" />

        @if (DataSource.File != null)
        {{
            <div class=""mt-2 storageFileUploadPreview"">
                <span>📄 @(DataSource.File.FileName)</span>
                <button type=""button"" class=""btn btn-sm btn-link text-danger"" @onclick=""DataSource.CancelFileSelected"">❌ Remove file</button>
            </div>
        }}
    </div>";
    }
}
