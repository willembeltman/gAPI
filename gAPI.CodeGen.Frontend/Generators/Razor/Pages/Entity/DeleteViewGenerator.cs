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
        ISharedReference iClientAuthenticatedHttpClient,
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
        IClientAuthenticatedHttpClient = iClientAuthenticatedHttpClient;
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
    public ISharedReference IClientAuthenticatedHttpClient { get; }
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
        Imports.Reg(IClientAuthenticatedHttpClient);
        Imports.Reg(RedirectToLoginView);
        Imports.Reg("Microsoft.AspNetCore.Components.Authorization");

        var entityName = CrudlType.Name;
        var keyType = CrudlType.KeyProperty.TypeSimpleName;
        var pluralName = CrudlType.Name.ToMultiple().ToLower();

        var readName = CrudlType.ReadMethod.Interface.Name.ToCamelCase();
        var deleteName = CrudlType.DeleteMethod.Interface.Name.ToCamelCase();

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
@implements IAsyncDisposable{GetRazorNamespacesCode()}{(readName == deleteName
    ? $@"
@inject {CrudlType.ReadMethod.Interface.Name} {readName}"
    : $@"
@inject {CrudlType.ReadMethod.Interface.Name} {readName}
@inject {CrudlType.DeleteMethod.Interface.Name} {deleteName}")}
@inject {IClientAuthenticatedHttpClient.Name} ClientAuthenticatedHttpClient
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

    private CancellationTokenSource? Cts;
    private {ItemDataSource.Name}<{entityName}, {keyType}>? {entityName};

    protected override async Task OnParametersSetAsync()
    {{
        if (Cts != null)
        {{
            await Cts.CancelAsync();
            Cts.Dispose();
        }}

        Cts = new CancellationTokenSource();

        if (await ClientAuthenticatedHttpClient.IsAuthenticatedAsync(Cts.Token) == false || id == null)
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
            FileDelete: {(CrudlType.IsStorageFileUrlProperty ? $"{deleteName}.FileDelete" : "null")}
        );

        await {entityName}.LoadModelAsync({idGetter});
    }}

    public async ValueTask DisposeAsync()
    {{
        if (Cts != null)
        {{
            await Cts.CancelAsync();
            Cts.Dispose();
        }}
        if ({entityName} != null) 
            await {entityName}.DisposeAsync();
    }}
}}";

        Save();
    }

}
