using gAPI.AutoComponents.Interfaces;
using gAPI.CodeGen.Frontend.Helpers;
using gAPI.CodeGen.Frontend.Models.CrudlsModels;

namespace gAPI.CodeGen.Frontend.Generators.Razor.Pages.Entity;

public class IndexViewGenerator : BaseGenerator
{
    public IndexViewGenerator(
        CrudlType crudlType,
        ISharedReference itemDataSource,
        ISharedReference listDataSource,
        ISharedReference baseResponse,
        ISharedReference baseResponseT,
        ISharedReference baseListResponseT,
        ISharedReference iClientAuthenticationService,
        ISharedReference listView,
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
        IClientAuthenticationService = iClientAuthenticationService;
        ListView = listView;
        LoaderView = loaderView;
        RedirectToLoginView = redirectToLoginView;
        Imports = imports;

        Directory = directory;
        Namespace = $"{@namespace}.{crudlType.Name!.ToMultiple()}";
        Name = "Index";
        FileName = $"{crudlType.Name!.ToMultiple()}\\{Name}.razor";
    }

    public CrudlType CrudlType { get; }
    public ISharedReference ItemDataSource { get; }
    public ISharedReference ListDataSource { get; }
    public ISharedReference IClientAuthenticationService { get; }
    public ISharedReference ListView { get; }
    public ISharedReference LoaderView { get; }
    public ISharedReference RedirectToLoginView { get; }
    public ImportsGenerator Imports { get; }

    public void GenerateCode()
    {
        if (CrudlType.ListMethod == null) return;

        // Imports registreren
        Imports.Reg(ListView);
        Imports.Reg(LoaderView);
        Imports.Reg(RedirectToLoginView);
        Imports.Reg(ListDataSource);
        Imports.Reg(CrudlType);
        Imports.Reg(CrudlType.ListMethod.Client!.Interface);
        Imports.Reg(IClientAuthenticationService);
        Imports.Reg("Microsoft.AspNetCore.Components.Authorization");

        var pluralName = CrudlType.Name!.ToMultiple().ToLower();
        var pluralTitle = CrudlType.Name.ToMultiple();
        var serviceName = CrudlType.ListMethod.Client.Name;
        var interfaceName = CrudlType.ListMethod.Client.Interface.Name;
        var entityName = CrudlType.Name;
        var keyType = CrudlType.KeyProperty.TypeSimpleName;

        Code = $@"@page ""/{pluralName}""
@implements IAsyncDisposable
{GetRazorNamespacesCode()}@inject {interfaceName} {serviceName}
@inject {IClientAuthenticationService.Name} ClientAuthenticationService
@inject IJSRuntime JS

<PageTitle>{pluralTitle}</PageTitle>

<AuthorizeView>
    <Authorized>
        <h3>{pluralTitle}</h3>
        <{LoaderView.Name} Response=""{entityName.ToMultiple()}?.Response"">
            @if ({entityName.ToMultiple()}!.Response!.CanCreate)
            {{
                <a id=""createnew"" href=""/{pluralName}/create"">➕ Create a new {entityName.ToLower()}</a>
            }}
            <{ListView.Name} DataSource=""{entityName.ToMultiple()}"" />
        </{LoaderView.Name}>
    </Authorized>
    <NotAuthorized>
        <{RedirectToLoginView.Name} />
    </NotAuthorized>
</AuthorizeView>

@code {{
    private {ListDataSource.Name}<{entityName}, {keyType}>? {entityName.ToMultiple()};

    protected override async Task OnInitializedAsync()
    {{
        if (await ClientAuthenticationService.IsAuthenticated() == false)
        {{
            return;
        }}

        {entityName.ToMultiple()} = new {ListDataSource.Name}<{entityName}, {keyType}>(
            JS,
            StateHasChanged,
            GetPrimaryKey: {entityName.ToLower()} => {entityName.ToLower()}.{CrudlType.KeyProperty.Name ?? "Id"},
            List: {serviceName}.List,
            SetForeignKey: null,
            AfterSaveAction: null,
            AfterCancelAction: null,
            Create: null,
            Read: null,
            Update: null,
            Delete: null
        );

        await {entityName.ToMultiple()}.InitialiseAsync();
    }}

    public async ValueTask DisposeAsync()
    {{
        if ({entityName.ToMultiple()} != null)
            await {entityName.ToMultiple()}.DisposeAsync();
    }}
}}";

        Save();
    }
}
