using gAPI.AutoComponent.Helpers;
using gAPI.AutoComponent.Interfaces;
using System.Linq;

namespace gAPI.AutoComponent.Generators.Components;

public class ListGenerator : BaseGenerator
{
    public ListGenerator(
        ICrudlType dto,
        ISharedReference itemDataSource,
        ISharedReference listDataSource,
        ISharedReference baseListResponse,
        IBaseGenerator imports,
        string directory,
        string @namespace) : base(directory, @namespace)
    {
        CrudlType = dto;
        ItemDataSource = itemDataSource;
        ListDataSource = listDataSource;
        BaseListResponse = baseListResponse;
        Imports = imports;

        Name = $"{dto.Name}List";
        FileName = $"{Name}.razor";
    }

    public ICrudlType CrudlType { get; }
    public IBaseGenerator Imports { get; }
    public ISharedReference ItemDataSource { get; }
    public ISharedReference ListDataSource { get; }
    public ISharedReference BaseListResponse { get; }

    public void GenerateCode()
    {
        if (CrudlType.ListMethod == null) return;

        // Imports
        Imports.RegRange(
        [
            "Microsoft.AspNetCore.Components",
            "System",
            "System.Linq",
            "System.Threading.Tasks",
            "System.Collections.Generic"
        ]);
        Imports.Reg(CrudlType);
        Imports.Reg(ItemDataSource);
        Imports.Reg(ListDataSource);
        Imports.Reg(BaseListResponse);

        // Genereer dynamisch de kolommen
        var orderableProps = CrudlType.Properties
            .Where(p => p.IsName || p.IsForeignName)
            .ToArray();

        var storageFileProps = CrudlType.Properties
            .Where(p => p.IsStorageFile)
            .ToArray();

        var displayProps = storageFileProps.Concat(orderableProps).ToArray();

        var orderLinks = string.Join("", displayProps.Select(p =>
        {
            if (p.IsStorageFile)
            {
                return $@"
                    @if (HideColumnNames.Contains(""{p.Name}"") == false)
                    {{
                        <th></th>
                    }}";
            }
            else
            {
                return $@"
                    @if (HideColumnNames.Contains(""{p.Name}"") == false)
                    {{
                        <th>
                            <a @onclick=""OrderBy{p.Name}Clicked"" id=""orderBy{p.Name}Button"" style=""cursor:pointer;"">
                                {p.Name}
                                @if (DataSource.OrderByColumn == ""{p.Name} asc"")
                                {{
                                    <span>▲</span>
                                }}
                                else if (DataSource.OrderByColumn == ""{p.Name} desc"")
                                {{
                                    <span>▼</span>
                                }}
                            </a>
                        </th>
                    }}";
            }
        }));

        var dataCells = string.Join("", displayProps.Select(p =>
        {
            if (p.IsStorageFile)
            {
                return $@"
                        @if (HideColumnNames.Contains(""{p.Name}"") == false)
                        {{
                            <td>
                                @if (!string.IsNullOrWhiteSpace(item.Model!.{p.Name}))
                                {{
                                    <img src=""@item.Model!.{p.Name}"" style=""max-height: 32px;"" />
                                }} 
                            </td>
                        }}";
            }
            else
            {
                return $@"
                        @if (HideColumnNames.Contains(""{p.Name}"") == false)
                        {{
                            <td>@item.Model!.{p.Name}</td>
                        }}";
            }
        }));

        var orderOptionsMobile = string.Join("", orderableProps.Select(p => $@"
                @if (HideColumnNames.Contains(""{p.Name}"") == false)
                {{
                    <option value=""{p.Name}"" selected=""@(DataSource.OrderByColumn == ""{p.Name}"")"">{p.Name}</option>
                }}"));

        var mobileCards = string.Join("", displayProps.Select(p =>
        {
            if (p.IsStorageFile)
            {
                return $@"
                                @if (!string.IsNullOrWhiteSpace(item.Model!.{p.Name}) && 
                                    HideColumnNames.Contains(""{p.Name}"") == false)
                                {{
                                    <img src=""@item.Model!.{p.Name}"" style=""max-height: 32px; margin-right: 8px;"" />
                                }}";
            }
            else
            {
                return $@"
                                @if (HideColumnNames.Contains(""{p.Name}"") == false)
                                {{
                                    <div class=""text-muted"">@item.Model!.{p.Name}</div>
                                }}";
            }
        }));

        // Razor code genereren
        Code = $@"@if (DataSource == null || DataSource.Items.Count == 0)
{{
    @if (DataSource == null || DataSource.HasMore == false)
    {{
        <p><em>@(NoItemsText)</em></p>
    }}
    else
    {{
        <p><em>@(LoadingText)</em></p>
    }}
}}
else
{{
    <!-- Desktop weergave -->
    <div class=""d-none d-md-block"">
        <table class=""table table-bordered"">
            <thead>
                <tr>{orderLinks}
                    <th></th>
                </tr>
            </thead>
            <tbody>
                @foreach (var item in DataSource.Items)
                {{
                    <tr>{dataCells}
                        <td>
                            @if (item.Model!.CanUpdate)
                            {{
                                <a class=""btn btn-sm btn-primary"" href=""/{CrudlType.Name.ToLower().ToMultiple()}/edit/@item.Model!.Id"">✏️</a>
                            }}
                            @if (item.Model!.CanDelete)
                            {{
                                <a class=""btn btn-sm btn-danger ms-1"" href=""/{CrudlType.Name.ToLower().ToMultiple()}/delete/@item.Model!.Id"">🗑️</a>
                            }}
                        </td>
                    </tr>
                }}
                @if (DataSource.HasMore)
                {{
                    <!-- sentinel om ook op desktop te laden -->
                    <tr id=""@DataSource.SentinelId"">
                        <td colspan=""@({displayProps.Length + 1} - HideColumnNames.Length)"" class=""text-center text-muted"">@(LoadingModeText)</td>
                    </tr>
                }}
            </tbody>
        </table>
    </div>

    <!-- Mobiele weergave -->
    <div class=""d-md-none"">
        <div class=""mb-2 d-flex gap-2 align-items-center"">
            <select @onchange=""DataSource.OrderByChanged"" class=""form-select form-select-sm w-auto"">
                <option value="""" selected=""@string.IsNullOrEmpty(DataSource.OrderByColumn)"">@EmptyText</option>{orderOptionsMobile}
            </select>
            @if (!string.IsNullOrEmpty(DataSource.OrderByColumn))
            {{
                <select @onchange=""DataSource.OrderByDirectionChanged"" class=""form-select form-select-sm w-auto"">
                    <option value=""false"" selected=""@(!DataSource.OrderByDirectionDesc)"">@AscText</option>
                    <option value=""true"" selected=""@DataSource.OrderByDirectionDesc"">@DescText</option>
                </select>
            }}
        </div>

        <div class=""row row-cols-1 g-2"">
            @foreach (var item in DataSource.Items)
            {{
                <div class=""col"">
                    <div class=""card shadow-sm"">
                        <div class=""card-body"">
                            <div class=""d-flex align-items-center"">{mobileCards}
                            </div>
                            <div class=""mt-2"">
                                @if (item.Model!.CanUpdate)
                                {{
                                    <a class=""btn btn-sm btn-primary"" href=""/{CrudlType.Name.ToLower().ToMultiple()}/edit/@item.Model!.Id"">@EditText</a>
                                }}
                                @if (item.Model!.CanDelete)
                                {{
                                    <a class=""btn btn-sm btn-danger ms-1"" href=""/{CrudlType.Name.ToLower().ToMultiple()}/delete/@item.Model!.Id"">@DeleteText</a>
                                }}
                            </div>
                        </div>
                    </div>
                </div>
            }}

            @if (DataSource.HasMore)
            {{
                <div id=""@DataSource.SentinelId"" class=""sentinel"">@(LoadingModeText)</div>
            }}
        </div>
    </div>
}}

@code {{
    [Parameter, EditorRequired]
    public ListDataSource<{CrudlType.Name}, {CrudlType.KeyProperty.TypeSimpleName}> DataSource {{ get; set; }}

    [Parameter] public string HideColumns {{ get; set; }} = string.Empty;
    [Parameter] public string Id {{ get; set; }} = $""{CrudlType.Name.ToLower()}List_{{Guid.NewGuid()}}"";
    [Parameter] public string EmptyText {{ get; set; }} = ""Order by..."";
    [Parameter] public string AscText {{ get; set; }} = ""▲ asc"";
    [Parameter] public string DescText {{ get; set; }} = ""▼ desc"";
    [Parameter] public string EditText {{ get; set; }} = ""✏️ Edit"";
    [Parameter] public string DeleteText {{ get; set; }} = ""🗑️ Delete"";
    [Parameter] public string LoadingText {{ get; set; }} = ""Loading, please wait..."";
    [Parameter] public string LoadingModeText {{ get; set; }} = ""Loading more..."";
    [Parameter] public string NoItemsText {{ get; set; }} = ""No {CrudlType.Name.ToMultiple()} to display."";

    private string[] HideColumnNames => HideColumns.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
{string.Join("", orderableProps.Select(p => $@"
    private async Task OrderBy{p.Name}Clicked() => await DataSource.OrderByColumnClicked(""{p.Name}"");"))}
}}";
    }
}
