using gAPI.AutoComponent.Helpers;
using gAPI.AutoComponent.Interfaces;
using gAPI.SimpleRazorCompiler;
using System;
using System.Linq;
using System.Reflection;

namespace gAPI.AutoComponent.Generators.Components;

public class GridEditGenerator : BaseGenerator
{
    public GridEditGenerator(
        ICrudlType dto,
        ISharedReference listDataSource,
        IBaseGenerator imports,
        string directory,
        string @namespace) 
    {
        CrudlType = dto;
        ListDataSource = listDataSource;
        Imports = imports;

        Directory = directory;
        Namespace = @namespace;

        Name = $"{dto.Name}GridEdit";
        FileName = $"{Name}.razor";
    }

    public ICrudlType CrudlType { get; }
    public ISharedReference ListDataSource { get; }
    public IBaseGenerator Imports { get; }

    public void GenerateCode()
    {
        Imports.RegRange(
        [
            "Microsoft.AspNetCore.Components",
            "Microsoft.AspNetCore.Components.Forms",
            "Microsoft.AspNetCore.Components.Web",
            "System",
            "System.Collections.Generic",
            "System.Linq",
            "System.Threading.Tasks"
        ]);

        Imports.Reg(CrudlType);
        Imports.Reg(ListDataSource);

        var properties = CrudlType.Properties
            .Where(p => !p.IsKey && !p.IsForeignName && !p.IsStateManaged && !p.IsImmutable && !p.IsReadOnly)
            .ToArray();
        var foreigns = properties
            .Where(p => p.ForeignKeyType != null && !p.IsStateManaged && !p.IsImmutable && !p.IsReadOnly)
            .ToArray();

        foreach (var p in properties)
        {
            Imports.Reg(p.TypeDigger);
            Imports.Reg(p.ForeignKeyType);
        }

        foreach (var f in foreigns)
        {
            Imports.Reg(f.ForeignKeyType!);
        }

        Code = $@"@if (DataSource == null || DataSource.Items.Count == 0)
{{
    @if (DataSource == null || DataSource.HasMore)
    {{
        <p><em>@(LoadingText)</em></p>
    }}
    else
    {{
        <p><em>@(NoItemsText)</em></p>
    }}
}}
else
{{
    <div class=""grid-container"" id=""@(Id)""
         style=""width:100%; max-height:250px; overflow:auto;"">
         
        <div class=""grid-row"">{(CrudlType.IsStorageFileUrlProperty ? @"
            <div class=""grid-header"">File</div>" : "")}{string.Join("", properties.Select(p => $@"
            @if (HideColumnNames.Contains(""{p.Name}"") == false)
            {{
                <div class=""grid-header"">{p.Name}</div>
            }}"))}
            <div class=""grid-header"">Actions</div>
        </div>

        @foreach (var item in DataSource.Items)
        {{
            <EditForm Model=""item.Model"" OnValidSubmit=""() => DataSource.HandleValidSubmit(item)"">
                <DataAnnotationsValidator />
                <div class=""grid-row"">{(CrudlType.IsStorageFileUrlProperty ? $@"
                    <div class=""grid-cell"">
                        @if (!string.IsNullOrWhiteSpace(item.Model!.StorageFileUrl))
                        {{
                            <div class=""storageFilePreview"">
                                <img src=""@(item.Model!.StorageFileUrl)"" style=""max-width: 100px;"" />
                                <button type=""button"" class=""btn btn-sm btn-link text-danger"" @onclick=""() => DataSource.OnHandleFileRemoved(item)"">❌ Remove</button>
                            </div>
                        }}
                        <div class=""storageFile"">
                        <InputFile OnChange=""(e) => DataSource.OnHandleFileSelected(item, e)"" key=""item.File.FileInputKey"" />
                            @if (item.File != null)
                            {{
                                <div class=""storageFileUploadPreview"">
                                    <span>📄 @item.File.FileName</span>
                                    <button type=""button"" class=""btn btn-sm btn-link text-danger"" @onclick=""() => DataSource.OnCancelFileSelected(item)"">❌ Remove</button>
                                </div>
                            }}
                        </div>
                    </div>" : "")}
                    {string.Join("\r\n                    ", properties.Select(p => GetPropertyCellMarkup(p)))}
                    <div class=""grid-cell"">
                        <button type=""submit"" class=""btn btn-primary btn-sm"">💾 Save</button>
                    </div>
                </div>
            </EditForm>
        }}

        @if (DataSource.HasMore)
        {{
            <div id=""@DataSource.SentinelId"" class=""sentinel"">@(LoadingMoreText)</div>
        }}
    </div>
}}

<style>
    .grid-container {{
    }}

    .grid-row {{
        font-size: 9px;
    }}

    .grid-header {{
        font-weight: bold;
        border-bottom: 1px solid #ccc;
        padding: 4px;
        display: inline-block;
        width: 100px;
    }}

    .grid-cell {{
        border-bottom: 1px solid #ddd;
        padding: 4px;
        display: inline-block;
        width: 100px;
    }}
</style>

@code {{
    [Parameter, EditorRequired]
    public {ListDataSource.Name}<{CrudlType.Name}, {CrudlType.KeyProperty.TypeSimpleName}>? DataSource {{ get; set; }}

    {string.Join("\r\n    ", foreigns.Select(f => $@"[Parameter, EditorRequired]
    public {ListDataSource.Name}<{f.ForeignKeyType!.Name}, {f.ForeignKeyType!.KeyProperty!.TypeSimpleName}>? {f.ForeignKeyType!.Name.ToMultiple()} {{ get; set; }}"))}

    [Parameter] public string Id {{ get; set; }} = $""{CrudlType.Name.ToLower()}GridEdit_{{Guid.NewGuid()}}"";
    [Parameter] public string LoadingText {{ get; set; }} = ""Loading, please wait..."";
    [Parameter] public string LoadingMoreText {{ get; set; }} = ""Loading more..."";
    [Parameter] public string NoItemsText {{ get; set; }} = ""No {CrudlType.Name.ToMultiple()} to display."";

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

        //var razorCode = Imports.GetRazorNamespacesCode() + "\r\n" + Code;
        //RazorCompiler.CompileRazorToComponent(razorCode, Namespace!, Name, Context.ServiceContext.Components);
    }

    private string GetPropertyCellMarkup(ICrudlProperty p)
    {
        var modelPrefix = "item.Model!";

        if (p.ForeignKeyType != null && p.ForeignKeyNameProperty != null)
        {
            var drop = p.PropertyType.IsNullable
                ? $@"@bind-NullableValue=""{modelPrefix}.{p.Name}"" bindtype_NullableValue=""{p.TypeSimpleName}"""
                : $@"@bind-Value=""{modelPrefix}.{p.Name}"" bindtype_Value=""{p.TypeSimpleName}""";

            return $@"@if (HideColumnNames.Contains(""{p.Name}"") == false)
                    {{
                        <div class=""grid-cell"">
                            <{p.ForeignKeyType.Name}DropDown 
                                {drop}
                                @bind-ForeignName=""{modelPrefix}.{p.ForeignKeyNameProperty.Name}"" bindtype_ForeignName=""string?"" 
                                DataSource=""{p.ForeignKeyType.Name.ToMultiple()}"" />
                        </div>
                    }}";
        }

        if (p.IsEnum)
        {
            return $@"@if (HideColumnNames.Contains(""{p.Name}"") == false)
                    {{
                        <div class=""grid-cell"">
                            <InputSelect @bind-Value=""{modelPrefix}.{p.Name}"" bindtype_Value=""{p.TypeSimpleName}""
                                class=""form-select"">
                                @foreach (var value in Enum.GetValues(typeof({p.TypeDigger.FullName})).Cast<{p.TypeDigger.FullName}>())
                                {{
                                    <option value=""@(value)"">@(value.ToString())</option>
                                }}
                            </InputSelect>
                        </div>
                    }}";
        }

        var input = p.IsCheckbox ? "InputCheckbox"
                   : p.IsNumber ? "InputNumber"
                   : p.IsDateTime ? "InputDate"
                   : "InputText";

        return $@"@if (HideColumnNames.Contains(""{p.Name}"") == false)
                    {{
                        <div class=""grid-cell"">
                            <{input} @bind-Value=""{modelPrefix}.{p.Name}"" bindtype_Value=""{p.TypeSimpleName}"" class=""form-control"" />
                        </div>
                    }}";
    }

}
