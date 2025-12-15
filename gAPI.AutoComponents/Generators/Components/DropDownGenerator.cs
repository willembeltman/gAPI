using gAPI.AutoComponents.Helpers;
using gAPI.AutoComponents.Interfaces;

namespace gAPI.AutoComponents.Generators.Components;

public class DropDownGenerator : BaseGenerator
{
    public DropDownGenerator(
        ICrudlType crudlType,
        ISharedReference listDataSource,
        IBaseGenerator imports,
        string directory,
        string @namespace) : base(directory, @namespace)
    {
        CrudlType = crudlType;
        ListDataSource = listDataSource;
        Imports = imports;

        Name = $"{crudlType.Name}DropDown";
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
            "System",
            "System.Collections.Generic",
            "System.Threading.Tasks"
        ]);

        Imports.Reg(ListDataSource);
        Imports.Reg(CrudlType.Namespace);

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
    <div class=""position-relative"">
        <button type=""button""
                id=""@Id""
                class=""form-control text-start d-flex justify-content-between align-items-center""
                @onclick=""DataSource.ToggleDropdown"">
            <span>@(ForeignName ?? ChooseText)</span>
            <span class=""ms-2"">▾</span>
        </button>

        <div class=""card mt-1""
             style=""position:absolute; z-index:2000; width:100%; max-height:250px; overflow:auto;@(DataSource.ShowDropdown ? """" : "" display:none;"")"">

            <div class=""list-group-item list-group-item-action"" style=""cursor:pointer;""
                 @onclick=""() => SelectItem(null)"">
                @(ChooseText)
            </div>

            @foreach (var item in DataSource.Items)
            {{
                <div class=""list-group-item list-group-item-action"" style=""cursor:pointer;""
                     @onclick=""() => SelectItem(item)"">
                    @item.Model!.ToString()
                </div>
            }}

            @if (DataSource.HasMore)
            {{
                <div id=""@(DataSource.SentinelId)"" class=""sentinel"">@(LoadingMoreText)</div>
            }}
        </div>
    </div>
}}

@code {{
    [Parameter, EditorRequired]
    public ListDataSource<{CrudlType.Name}, {CrudlType.KeyProperty.TypeSimpleName}>? DataSource {{ get; set; }} = default!;

    [Parameter] public {CrudlType.KeyProperty.TypeSimpleName} Value {{ get; set; }}
    [Parameter] public EventCallback<{CrudlType.KeyProperty.TypeSimpleName}> ValueChanged {{ get; set; }}
    [Parameter] public string? bindtype_Value {{ get; set; }}

    [Parameter] public {CrudlType.KeyProperty.TypeSimpleName}? NullableValue {{ get; set; }}
    [Parameter] public EventCallback<{CrudlType.KeyProperty.TypeSimpleName}?> NullableValueChanged {{ get; set; }}
    [Parameter] public string? bindtype_NullableValue {{ get; set; }}

    [Parameter] public string? ForeignName {{ get; set; }}
    [Parameter] public EventCallback<string?> ForeignNameChanged {{ get; set; }}
    [Parameter] public string? bindtype_ForeignName {{ get; set; }}

    [Parameter] public string? Id {{ get; set; }} = $""{CrudlType.Name.ToLower()}Dropdown_{{Guid.NewGuid()}}""; 

    [Parameter] public string ChooseText {{ get; set; }} = ""-- Choose a {CrudlType.Name.ToLower()} --"";
    [Parameter] public string LoadingText {{ get; set; }} = ""Loading {CrudlType.Name.ToMultiple().ToLower()}..."";
    [Parameter] public string NoItemsText {{ get; set; }} = ""No {CrudlType.Name.ToMultiple().ToLower()} available."";
    [Parameter] public string LoadingMoreText {{ get; set; }} = ""Loading more..."";

    private async Task SelectItem(ItemDataSource<{CrudlType.Name}, {CrudlType.KeyProperty.TypeSimpleName}>? item)
    {{
        if (DataSource == null) return;

        if (item?.Model == null)
        {{
            Value = default;
            NullableValue = default;
            ForeignName = null;
        }}
        else
        {{
            Value = item.Model.{CrudlType.KeyProperty.Name};
            NullableValue = item.Model.{CrudlType.KeyProperty.Name};
            ForeignName = item.Model.ToString();
        }}

        await Task.WhenAll(
            ValueChanged.InvokeAsync(Value),
            NullableValueChanged.InvokeAsync(NullableValue),
            ForeignNameChanged.InvokeAsync(ForeignName)
        );

        await DataSource.CloseDropdown(item);
    }}
}}";
    }
}
