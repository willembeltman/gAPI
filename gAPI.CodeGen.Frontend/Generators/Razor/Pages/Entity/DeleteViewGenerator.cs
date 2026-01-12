using gAPI.AutoComponent.Interfaces;
using gAPI.CodeGen.Frontend.Helpers;
using gAPI.CodeGen.Frontend.Models.CrudlsModels;

namespace gAPI.CodeGen.Frontend.Generators.Razor.Pages.Entity;

public class DeleteViewGenerator : BaseGenerator
{
    public DeleteViewGenerator(
        CrudlType crudlType,
        ISharedReference itemDataSource,
        ISharedReference listDataSource,
        ISharedReference baseResponse,
        ISharedReference baseResponseT,
        ISharedReference baseListResponseT,
        ISharedReference iClientAuthenticationService,
        ISharedReference detailsView,
        ISharedReference loaderView,
        ISharedReference errorView,
        ISharedReference redirectToLoginView,
        ImportsGenerator imports,
        DirectoryInfo directory,
        string? @namespace)
    {
        CrudlType = crudlType;
        ItemDataSource = itemDataSource;
        ListDataSource = listDataSource;
        BaseResponseT = baseResponseT;
        BaseListResponseT = baseListResponseT;
        IClientAuthenticationService = iClientAuthenticationService;
        DetailsView = detailsView;
        LoaderView = loaderView;
        ErrorView = errorView;
        RedirectToLoginView = redirectToLoginView;
        Imports = imports;

        Directory = directory;
        Namespace = $"{@namespace}.{crudlType.Name!.ToMultiple()}";
        Name = "Delete";

        FileName = $"{crudlType.Name!.ToMultiple()}\\{Name}.razor";
    }

    public CrudlType CrudlType { get; }
    public ISharedReference ItemDataSource { get; }
    public ISharedReference ListDataSource { get; }
    public ISharedReference DetailsView { get; }
    public ISharedReference BaseResponseT { get; }
    public ISharedReference BaseListResponseT { get; }
    public ISharedReference IClientAuthenticationService { get; }
    public ISharedReference ErrorView { get; }
    public ISharedReference RedirectToLoginView { get; }
    public ISharedReference LoaderView { get; }
    public ImportsGenerator Imports { get; }
    public void GenerateCode()
    {
        if (CrudlType.DeleteMethod == null ||
            CrudlType.ReadMethod == null ||
            CrudlType.Name == null)
            return;

        Imports.Reg(CrudlType);
        Imports.Reg(DetailsView);
        Imports.Reg(BaseResponseT);
        Imports.Reg(IClientAuthenticationService);
        Imports.Reg(RedirectToLoginView);
        Imports.Reg("Microsoft.AspNetCore.Components.Authorization");

        var entityName = CrudlType.Name;
        var keyType = CrudlType.KeyProperty.TypeSimpleName;
        var pluralName = CrudlType.Name.ToMultiple().ToLower();

        var readName = CrudlType.ReadMethod.Client!.Name;
        var deleteName = CrudlType.DeleteMethod.Client!.Name;

        var idGetter = "id";
        var paramRouteType = $":{keyType}?";
        var paramType = $"{keyType}?";
        if (paramType == "Guid?")
        {
            paramType = "string?";
            idGetter = $"Guid.TryParse(id, out var guidId) ? guidId : null";
        }
        if (paramType == "string?")
        {
            paramRouteType = "?";
        }


        Code = $@"@page ""/{pluralName}/delete/{{id{paramRouteType}}}""
{GetRazorNamespacesCode()}{GetRazorNamespacesCode()}{(readName == deleteName
            ? $@"@inject {CrudlType.ReadMethod.Client.Interface.Name} {readName}"
            : $@"@inject {CrudlType.ReadMethod.Client.Interface.Name} {readName}
@inject {CrudlType.DeleteMethod.Client.Interface.Name} {deleteName}")}
@inject {IClientAuthenticationService.Name} ClientAuthenticationService
@inject IJSRuntime JS
@inject NavigationManager NavigationManager

<PageTitle>Delete {entityName}</PageTitle>

<AuthorizeView>
    <Authorized>
        <h3>Delete {entityName}</h3>
        <LoaderView Response=""{entityName}?.Response"">
            <div class=""alert alert-warning"">
                Are you sure you want to delete this {entityName}
            </div>

            <{DetailsView.Name} DataSource=""{entityName}"" />
            <ErrorView Response=""{entityName}!.StatusResponse"" />

            <button id=""delete"" class=""btn btn-danger"" @onclick=""{entityName}.HandleDelete"">🗑️ Delete</button>
            <button id=""cancel"" class=""btn btn-secondary ms-2"" @onclick=""{entityName}.Cancel"">Cancel</button>
        </LoaderView>
    </Authorized>
    <NotAuthorized>
        <RedirectToLogin />
    </NotAuthorized>
</AuthorizeView>

@code {{
    [Parameter]
    public {paramType} id {{ get; set; }}

    private {ItemDataSource.Name}<{entityName}, {keyType}>? {entityName};

    protected override async Task OnInitializedAsync()
    {{
        if (await ClientAuthenticationService.IsAuthenticatedAsync() == false || id == null)
        {{
            return;
        }}

        {entityName} = new {ItemDataSource.Name}<{entityName}, {keyType}>(
            GetPrimaryKey: item => item.{CrudlType.KeyProperty.Name ?? "Id"},
            AfterSaveAction: item => NavigationManager.NavigateTo(""/{pluralName}""),
            AfterCancelAction: item => NavigationManager.NavigateTo(""/{pluralName}""),
            Create: null,
            Read: {readName}.Read,
            Update: null,
            Delete: {deleteName}.Delete,
            FileUpdate: null,
            FileDelete: {(CrudlType.IsStorageFile ? $"{deleteName}.FileDelete" : "null")}
        );

        await {entityName}.LoadModelAsync({idGetter});
    }}
}}";

        Save();
    }

}
