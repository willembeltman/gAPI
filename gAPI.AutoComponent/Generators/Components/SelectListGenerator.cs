using gAPI.AutoComponent.Helpers;
using gAPI.AutoComponent.Interfaces;
using System.Linq;

namespace gAPI.AutoComponent.Generators.Components;

public class SelectListGenerator : BaseGenerator
{
    public SelectListGenerator(
        ICrudType dto,
        ISharedReference itemDataSource,
        ISharedReference listDataSource,
        ISharedReference baseListResponse,
        IBaseGenerator imports,
        string directory,
        string @namespace)
    {
        CrudType = dto;
        ItemDataSource = itemDataSource;
        ListDataSource = listDataSource;
        BaseListResponse = baseListResponse;
        Imports = imports;

        Directory = directory;
        Namespace = @namespace;

        Name = $"{dto.Name}SelectList";
        FileName = $"{Name}.razor";
    }

    public ICrudType CrudType { get; }
    public IBaseGenerator Imports { get; }
    public ISharedReference ItemDataSource { get; }
    public ISharedReference ListDataSource { get; }
    public ISharedReference BaseListResponse { get; }

    public void GenerateCode()
    {
        if (CrudType.ListMethod == null) return;

        // Imports
        Imports.RegRange(
        [
            "Microsoft.AspNetCore.Components",
            "System",
            "System.Linq",
            "System.Threading.Tasks",
            "System.Collections.Generic"
        ]);

        Imports.Reg(CrudType);
        Imports.Reg(ItemDataSource);
        Imports.Reg(ListDataSource);
        Imports.Reg(BaseListResponse);

        // Alleen name / foreign-name / storage
        var orderableProps = CrudType.Properties
            .Where(p => p.IsName || p.IsForeignName)
            .OrderByDescending(a => a.IsName)
            .ThenByDescending(a => a.IsForeignName)
            .ToArray();

        var storageFileProps = CrudType.Properties
            .Where(p => p.IsStorageFileUrlProperty)
            .ToArray();

        var displayProps = storageFileProps.Concat(orderableProps).ToArray();

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
    <div id=""@Id"">
        <!-- Desktop -->
        <div class=""d-none d-md-block"">
            <table class=""table table-bordered"">
                <thead>
                    <tr>
                        <th></th>{string.Join("", displayProps.Select(p =>
            {
                if (p.IsStorageFileUrlProperty)
                {
                    return $@"
                        @if (!HideColumnNames.Contains(""{p.Name}""))
                        {{
                            <th></th>
                        }}";
                }

                return $@"
                    @if (!HideColumnNames.Contains(""{p.Name}""))
                    {{
                        <th>
                            <a @onclick=""OrderBy{p.Name}Clicked"" style=""cursor:pointer;"">
                                {p.Name}
                                @if (DataSource.OrderByColumn == ""{p.Name} asc"") {{ <span>▲</span> }}
                                else if (DataSource.OrderByColumn == ""{p.Name} desc"") {{ <span>▼</span> }}
                            </a>
                        </th>
                    }}";
            }))}
                    </tr>
                </thead>
                <tbody>
                    @foreach (var item in DataSource.Items)
                    {{
                        <tr>
                            <td>
                                <button class=""btn btn-sm btn-outline-primary""
                                        @onclick=""() => SelectItem(item)"">
                                    @SelectText
                                </button>
                            </td>{string.Join("", displayProps.Select(p =>
            {
                if (p.IsStorageFileUrlProperty)
                {
                    return $@"
                        @if (!HideColumnNames.Contains(""{p.Name}""))
                        {{
                            <td>
                                @if (!string.IsNullOrWhiteSpace(item.Model!.{p.Name}))
                                {{
                                    <img src=""@item.Model!.{p.Name}"" style=""max-height:32px;"" />
                                }}
                            </td>
                        }}";
                }

                return $@"
                    @if (!HideColumnNames.Contains(""{p.Name}""))
                    {{
                        <td>@item.Model!.{p.Name}</td>
                    }}";
            }))}
                        </tr>
                    }}

                    @if (DataSource.HasMore)
                    {{
                        <tr id=""@DataSource.SentinelId"">
                            <td colspan=""@SentinelWidth""
                                class=""text-center text-muted"">
                                @LoadingModeText
                            </td>
                        </tr>
                    }}
                </tbody>
            </table>
        </div>

        <!-- Mobile -->
        <div class=""d-md-none"">
            <div class=""mb-2 d-flex gap-2 align-items-center"">
                <select @onchange=""DataSource.OrderByChanged"" class=""form-select form-select-sm w-auto"">
                    <option value="""">@EmptyText</option>{string.Join("", orderableProps.Select(p => $@"
                    @if (!HideColumnNames.Contains(""{p.Name}""))
                    {{
                        <option value=""{p.Name}"" selected=""@{p.Name}Selected"">
                            {p.Name}
                        </option>
                    }}"))}
                </select>

                @if (!string.IsNullOrEmpty(DataSource.OrderByColumn))
                {{
                    <select @onchange=""DataSource.OrderByDirectionChanged""
                            class=""form-select form-select-sm w-auto"">
                        <option value=""false"" selected=""@DataSource.OrderByDirectionAsc"">@AscText</option>
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
                                <div class=""d-flex align-items-center"">{string.Join("", displayProps.Select(p =>
            {
                if (p.IsStorageFileUrlProperty)
                {
                    return $@"
                                    @if (!string.IsNullOrWhiteSpace(item.Model!.{p.Name}) &&
                                        !HideColumnNames.Contains(""{p.Name}""))
                                    {{
                                        <img src=""@item.Model!.{p.Name}"" style=""max-height:32px; margin-right:8px;"" />
                                    }}";
                }

                return $@"
                                    @if (!HideColumnNames.Contains(""{p.Name}""))
                                    {{
                                        <div class=""text-muted"">@item.Model!.{p.Name}</div>
                                    }}";
            }))}
                                </div>
                                <div class=""mt-2"">
                                    <button class=""btn btn-sm btn-outline-primary w-100""
                                            @onclick=""() => SelectItem(item)"">
                                        @SelectText
                                    </button>
                                </div>
                            </div>
                        </div>
                    </div>
                }}

                @if (DataSource.HasMore)
                {{
                    <div id=""@DataSource.SentinelId"" class=""sentinel"">
                        @LoadingModeText
                    </div>
                }}
            </div>
        </div>
    </div>
}}

@code {{
    [Parameter, EditorRequired]
    public ListDataSource<{CrudType.Name}, {CrudType.KeyProperty.TypeSimpleName}>? DataSource {{ get; set; }}

    [Parameter, EditorRequired]
    public EventCallback<ItemDataSource<{CrudType.Name}, {CrudType.KeyProperty.TypeSimpleName}>> OnSelect {{ get; set; }}

    [Parameter] public string Id {{ get; set; }} = $""{CrudType.Name.ToLower()}SelectList_{{Guid.NewGuid()}}"";
    [Parameter] public string SelectText {{ get; set; }} = ""Select"";
    [Parameter] public string EmptyText {{ get; set; }} = ""Order by..."";
    [Parameter] public string AscText {{ get; set; }} = ""▲ asc"";
    [Parameter] public string DescText {{ get; set; }} = ""▼ desc"";
    [Parameter] public string LoadingModeText {{ get; set; }} = ""Loading more..."";
    [Parameter] public string LoadingText {{ get; set; }} = ""Loading, please wait..."";
    [Parameter] public string NoItemsText {{ get; set; }} = ""No {CrudType.Name.ToMultiple()} to select."";

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

    private async Task SelectItem(ItemDataSource<{CrudType.Name}, {CrudType.KeyProperty.TypeSimpleName}> item)
    {{
        if (OnSelect.HasDelegate)
            await OnSelect.InvokeAsync(item);
    }}
    {string.Join("", orderableProps.Select(p => $@"
    private async Task OrderBy{p.Name}Clicked()
    {{
        if (DataSource != null)
            await DataSource.OrderByColumnClicked(""{p.Name}"");
    }}"))}
    private int SentinelWidth => {displayProps.Length + 1} - HideColumnNames.Length;{string.Join("", orderableProps.Select(p => $@"
    private bool {p.Name}Selected => DataSource?.OrderByColumn == ""{p.Name}"";"))}
}}";
    }
}
