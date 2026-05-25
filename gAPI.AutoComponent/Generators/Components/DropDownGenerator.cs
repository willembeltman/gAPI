using gAPI.AutoComponent.Helpers;
using gAPI.AutoComponent.Interfaces;

namespace gAPI.AutoComponent.Generators.Components;

public class DropDownGenerator : BaseGenerator
{
    public DropDownGenerator(
        ICrudType crudType,
        ISharedReference listDataSource,
        IBaseGenerator imports,
        string directory,
        string @namespace)
    {
        CrudType = crudType;
        ListDataSource = listDataSource;
        Imports = imports;

        Directory = directory;
        Namespace = @namespace;

        Name = $"{crudType.Name}DropDown";
        FileName = $"{Name}.razor";
    }

    public ICrudType CrudType { get; }
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
        Imports.Reg(CrudType.Namespace);

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
    public ListDataSource<{CrudType.Name}, {CrudType.KeyProperty.TypeSimpleName}>? DataSource {{ get; set; }} = default!;

    [Parameter] public {CrudType.KeyProperty.TypeSimpleName} Value {{ get; set; }}
    [Parameter] public EventCallback<{CrudType.KeyProperty.TypeSimpleName}> ValueChanged {{ get; set; }}
    [Parameter] public string? bindtype_Value {{ get; set; }}
    [Parameter] public System.Linq.Expressions.Expression<Func<{CrudType.KeyProperty.TypeSimpleName}>>? ValueExpression {{ get; set; }}

    [Parameter] public {CrudType.KeyProperty.TypeSimpleName}? NullableValue {{ get; set; }}
    [Parameter] public EventCallback<{CrudType.KeyProperty.TypeSimpleName}?> NullableValueChanged {{ get; set; }}
    [Parameter] public string? bindtype_NullableValue {{ get; set; }}
    [Parameter] public System.Linq.Expressions.Expression<Func<{CrudType.KeyProperty.TypeSimpleName}>>? NullableValueExpression {{ get; set; }}

    [Parameter] public string? ForeignName {{ get; set; }}
    [Parameter] public EventCallback<string?> ForeignNameChanged {{ get; set; }}
    [Parameter] public string? bindtype_ForeignName {{ get; set; }}
    [Parameter] public System.Linq.Expressions.Expression<Func<string?>>? ForeignNameExpression {{ get; set; }}

    [Parameter] public string? Id {{ get; set; }} = $""{CrudType.Name.ToLower()}Dropdown_{{Guid.NewGuid()}}""; 

    [Parameter] public string ChooseText {{ get; set; }} = ""-- Choose a {CrudType.Name.ToLower()} --"";
    [Parameter] public string LoadingText {{ get; set; }} = ""Loading {CrudType.Name.ToMultiple().ToLower()}..."";
    [Parameter] public string NoItemsText {{ get; set; }} = ""No {CrudType.Name.ToMultiple().ToLower()} available."";
    [Parameter] public string LoadingMoreText {{ get; set; }} = ""Loading more..."";

    private async Task SelectItem(ItemDataSource<{CrudType.Name}, {CrudType.KeyProperty.TypeSimpleName}>? item)
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
            Value = item.Model.{CrudType.KeyProperty.Name};
            NullableValue = item.Model.{CrudType.KeyProperty.Name};
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
