using gAPI.AutoComponent.Helpers;
using gAPI.AutoComponent.Interfaces;
using System.Linq;

namespace gAPI.AutoComponent.Generators.Components;

public class FormGenerator : BaseGenerator
{
    public FormGenerator(
        ICrudType dto,
        ISharedReference itemDataSource,
        ISharedReference listDataSource,
        ISharedReference iClientAuthenticatedHttpClient,
        ISharedReference formFile,
        ISharedReference toFormFileAsyncExtension,
        IBaseGenerator imports,
        string directory,
        string @namespace)
    {
        CrudType = dto;
        ItemDataSource = itemDataSource;
        ListDataSource = listDataSource;
        Imports = imports;
        IClientAuthenticatedHttpClient = iClientAuthenticatedHttpClient;
        FormFile = formFile;
        IsFormFileExtension = toFormFileAsyncExtension;

        Directory = directory;
        Namespace = @namespace;

        Name = $"{dto.Name}Form";
        FileName = $"{Name}.razor";
    }

    public IBaseGenerator Imports { get; }
    public ISharedReference IClientAuthenticatedHttpClient { get; }
    public ISharedReference FormFile { get; }
    public ISharedReference IsFormFileExtension { get; }
    public ICrudType CrudType { get; }
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
        Imports.Reg(CrudType);
        Imports.Reg(ItemDataSource);
        Imports.Reg(ListDataSource);
        Imports.Reg(IClientAuthenticatedHttpClient);

        if (CrudType.IsStorageFileUrlProperty)
        {
            Imports.Reg("Microsoft.AspNetCore.Http");
            Imports.Reg(FormFile);
            Imports.Reg(IsFormFileExtension);
        }

        var clients = CrudType.ForeignItemProperties
            .Where(p => !p.IsStateManaged)
            .ToArray();
        foreach (var client in clients)
        {
            Imports.Reg(client.ForeignKeyType);
            Imports.Reg(client.ListMethod?.Interface);
        }

        foreach (var p in CrudType.Properties)
        {
            Imports.Reg(p.TypeDigger);
            Imports.Reg(p.ForeignKeyType);
        }

        var properties = CrudType.Properties
            .Where(p =>
                !p.IsStateManaged &&
                !p.IsReadOnly &&
                !p.IsForeignName &&
                !p.IsKey)
            .ToArray();

        var propertySections = string.Join("\r\n", properties.Select(a => GetPropertyMarkup(a)));

        // Razor file output
        Code = $@"@if (DataSource?.Model == null)
{{
    <text><!-- niets te renderen --></text>
}}
else
{{{(CrudType.IsStorageFileUrlProperty ? $@"
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
    </div>" : "")}
{propertySections}
}}

@code {{
    [Parameter, EditorRequired]
    public {ItemDataSource.Name}<{CrudType.Name}, {CrudType.KeyProperty.TypeSimpleName}>? DataSource {{ get; set; }}
{string.Join("", clients.Select(p => $@"
    [Parameter, EditorRequired]
    public {ListDataSource.Name}<{p.ForeignKeyType!.Name}, {p.ForeignKeyType!.KeyProperty.TypeSimpleName}> {p.ForeignKeyType!.Name.ToMultiple()} {{ get; set; }} = null!;"))}

    private string[] HideColumnNames = [];

    [Parameter]
    public string? HideColumns {{ get; set; }}

    protected override void OnParametersSet()
    {{
        HideColumnNames = string.IsNullOrWhiteSpace(HideColumns)
            ? []
            : HideColumns.Split(
                ',',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
              );
    }}
}}";
    }

    private string GetPropertyMarkup(ICrudProperty p)
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
    @if (HideColumnNames.Contains(""{p.Name}"") == false)
    {{
        <div class=""mb-3"">
            <label for=""{id}"" class=""form-label"">{p.ForeignKeyType.Name}</label>
            <{p.ForeignKeyType.Name}DropDown @{bindAttr}=""{model}.{p.Name}"" {bindTypeAttr}=""{valueType}""
                @bind-ForeignName=""{model}.{p.ForeignKeyNameProperty.Name}"" bindtype_ForeignName=""string?""
                DataSource=""{dsName}"" id=""{id}"" />
        </div>
    }}";
        }

        if (p.IsNumber)
        {
            return $@"
    @if (HideColumnNames.Contains(""{p.Name}"") == false)
    {{
        <div class=""mb-3"">
            <label for=""{id}"" class=""form-label"">{p.Name}</label>
            <InputNumber @bind-Value=""{model}.{p.Name}"" bindtype_Value=""{p.TypeSimpleName}""
                id=""{id}"" class=""form-control"" />
        </div>
    }}";
        }

        if (p.IsDateTime)
        {
            return $@"
    @if (HideColumnNames.Contains(""{p.Name}"") == false)
    {{
        <div class=""mb-3"">
            <label for=""{id}"" class=""form-label"">{p.Name}</label>
            <InputDate @bind-Value=""{model}.{p.Name}"" bindtype_Value=""{p.TypeSimpleName}""
                id=""{id}"" class=""form-control"" />
        </div>
    }}";
        }

        if (p.IsCheckbox)
        {
            return $@"
    @if (HideColumnNames.Contains(""{p.Name}"") == false)
    {{
        <div class=""mb-3"">
            <label for=""{id}"" class=""form-label"">{p.Name}</label>
            <InputCheckbox @bind-Value=""{model}.{p.Name}"" bindtype_Value=""{p.TypeSimpleName}""
                id=""{id}"" class=""form-check-input"" />
        </div>
    }}";
        }

        if (p.IsEnum)
        {
            return $@"
    @if (HideColumnNames.Contains(""{p.Name}"") == false)
    {{
        <div class=""mb-3"">
            <label for=""{id}"" class=""form-label"">{p.Name}</label>
            <InputSelect @bind-Value=""{model}.{p.Name}"" bindtype_Value=""{p.TypeSimpleName}""
                id=""{id}"" class=""form-select"">
                @foreach (var value in Enum.GetValues(typeof({p.TypeDigger.FullName})).Cast<{p.TypeDigger.FullName}>())
                {{
                    <option value=""@(value)"">@(value.ToString())</option>
                }}
            </InputSelect>
        </div>
    }}";
        }

        // Default: InputText
        return $@"
    @if (HideColumnNames.Contains(""{p.Name}"") == false)
    {{
        <div class=""mb-3"">
            <label for=""{id}"" class=""form-label"">{p.Name}</label>
            <InputText @bind-Value=""{model}.{p.Name}"" bindtype_Value=""{p.TypeSimpleName}""
                id=""{id}"" class=""form-control"" />
        </div>
    }}";
    }

}
