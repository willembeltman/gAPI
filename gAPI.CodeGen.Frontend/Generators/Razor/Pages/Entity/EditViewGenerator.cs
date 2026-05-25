using gAPI.AutoComponent.Interfaces;
using gAPI.CodeGen.Frontend.Helpers;
using gAPI.CodeGen.Frontend.Models.CrudsModels;

namespace gAPI.CodeGen.Frontend.Generators.Razor.Pages.Entity;

public class EditViewGenerator : BaseGenerator
{
    public EditViewGenerator(
        CrudType crudType,
        ISharedReference itemDataSource,
        ISharedReference listDataSource,
        ISharedReference baseResponse,
        ISharedReference baseResponseT,
        ISharedReference baseListResponseT,
        ISharedReference iClientAuthenticatedHttpClient,
        ISharedReference formView,
        ISharedReference loaderView,
        ISharedReference errorView,
        ISharedReference redirectToLoginView,
        ImportsGenerator imports,
        DirectoryInfo directory,
        string? @namespace)
    {
        CrudType = crudType;
        ItemDataSource = itemDataSource;
        ListDataSource = listDataSource;
        BaseResponse = baseResponse;
        BaseResponseT = baseResponseT;
        BaseListResponseT = baseListResponseT;
        IClientAuthenticatedHttpClient = iClientAuthenticatedHttpClient;
        FormView = formView;
        LoaderView = loaderView;
        ErrorView = errorView;
        RedirectToLoginView = redirectToLoginView;
        Imports = imports;

        Directory = directory;
        Namespace = $"{@namespace}.{crudType.Name!.ToMultiple()}";
        Name = "Edit";

        FileName = $"{crudType.Name!.ToMultiple()}\\{Name}.razor";
    }

    public CrudType CrudType { get; }
    public ISharedReference ItemDataSource { get; }
    public ISharedReference ListDataSource { get; }
    public ISharedReference FormView { get; }
    public ISharedReference BaseResponse { get; }
    public ISharedReference BaseResponseT { get; }
    public ISharedReference BaseListResponseT { get; }
    public ISharedReference IClientAuthenticatedHttpClient { get; }
    public ISharedReference ErrorView { get; }
    public ISharedReference RedirectToLoginView { get; }
    public ISharedReference LoaderView { get; }
    public ImportsGenerator Imports { get; }

    public void GenerateCode()
    {
        if (CrudType.UpdateMethod == null ||
            CrudType.ReadMethod == null ||
            CrudType.Name == null)
            return;

        Imports.Reg(CrudType);
        Imports.Reg(ItemDataSource);
        Imports.Reg(ListDataSource);
        Imports.Reg(LoaderView);
        Imports.Reg(FormView);
        Imports.Reg(ErrorView);
        Imports.Reg(RedirectToLoginView);
        Imports.Reg(IClientAuthenticatedHttpClient);
        Imports.Reg("Microsoft.AspNetCore.Components");
        Imports.Reg("Microsoft.AspNetCore.Components.Forms");
        Imports.Reg("Microsoft.JSInterop");

        var entityName = CrudType.Name;
        var keyType = CrudType.KeyProperty.TypeSimpleName;
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

        var clients = CrudType.ForeignItemProperties
            .Where(p => !p.IsStateManaged && !p.IsReadOnly)
            .ToArray();
        foreach (var p in clients)
            Imports.Reg(p.ListMethod!.Interface.Namespace);



        Code = $@"@page ""/{entityName.ToLower().ToMultiple()}/edit/{{id{paramRouteType}}}""
@implements IAsyncDisposable{GetRazorNamespacesCode()}{string.Join("", clients.Select(p => $@"
@inject {p.ListMethod!.Interface.Name} {p.ListMethod.Name.ToCamelCase()}"))}
@inject {CrudType.UpdateMethod.Interface.Name} {CrudType.UpdateMethod.Name.ToCamelCase()}
@inject {IClientAuthenticatedHttpClient.Name} ClientAuthenticatedHttpClient
@inject IJSRuntime JS
@inject NavigationManager NavigationManager

<PageTitle>Edit {entityName}</PageTitle>

<AuthorizeView>
    <Authorized Context=""_"">
        <h3>Edit {entityName}</h3>
        <{LoaderView.Name} Response=""{entityName}?.Response"">
            <EditForm Model=""{entityName}!.Model"" OnValidSubmit=""{entityName}.HandleValidSubmit"">
                <DataAnnotationsValidator />
                <ValidationSummary />
                <{FormView.Name} DataSource=""{entityName}""{string.Join("", clients.Select(p => $@" {p.ForeignKeyType!.Name.ToMultiple()}=""{p.ForeignKeyType.Name.ToMultiple()}"""))}{(CrudType.ForeignItemProperties.Any(p => p.IsImmutable) ? $@" HideColumns=""{string.Join(", ", CrudType.ForeignItemProperties
                .Where(p => p.IsImmutable)
                .Select(p => p.Name))}""" : "")} />
                <{ErrorView.Name} Response=""{entityName}.StatusResponse"" />
                <button id=""save"" class=""btn btn-primary"" type=""submit"">💾 Save</button>
                <button id=""cancel"" class=""btn btn-secondary ms-2"" @onclick=""{entityName}.Cancel"">Cancel</button>
            </EditForm>
        </{LoaderView.Name}>
    </Authorized>
    <NotAuthorized>
        <{RedirectToLoginView.Name} />
    </NotAuthorized>
</AuthorizeView>

@code {{
    [Parameter]
    public {paramType} id {{ get; set; }}

    private CancellationTokenSource? Cts;
    private {ItemDataSource.Name}<{entityName}, {keyType}>? {entityName};{string.Join("", clients.Select(p => @$"
    private {ListDataSource.Name}<{p.ForeignKeyType!.Name}, {p.ForeignKeyType.KeyProperty!.TypeSimpleName}>? {p.ForeignKeyType.Name.ToMultiple()};"))}

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

        {entityName} = new {ItemDataSource.Name}<{entityName}, {CrudType.KeyProperty.TypeSimpleName}>(
            GetPrimaryKey: e => e.{CrudType.KeyProperty.Name},
            AfterSaveAction: e => NavigationManager.NavigateTo(""/{entityName.ToLower().ToMultiple()}""),
            AfterCancelAction: e => NavigationManager.NavigateTo(""/{entityName.ToLower().ToMultiple()}""),
            Create: {CrudType.UpdateMethod.Name.ToCamelCase()}.Create,
            Read: {CrudType.UpdateMethod.Name.ToCamelCase()}.Read,
            Update: {CrudType.UpdateMethod.Name.ToCamelCase()}.Update,
            Delete: {CrudType.UpdateMethod.Name.ToCamelCase()}.Delete,
            FileUpdate: {(CrudType.IsStorageFileUrlProperty ? $"{CrudType.UpdateMethod.Name.ToCamelCase()}.FileUpdate" : "null")},
            FileDelete: {(CrudType.IsStorageFileUrlProperty ? $"{CrudType.UpdateMethod.Name.ToCamelCase()}.FileDelete" : "null")}
        );
        await {entityName}.LoadModelAsync({idGetter});{string.Join("", clients.Select(p => $@"

        {p.ForeignKeyType!.Name.ToMultiple()} = new {ListDataSource.Name}<{p.ForeignKeyType.Name}, {p.ForeignKeyType.KeyProperty!.TypeSimpleName}>(
            JS,
            StateHasChanged,
            GetPrimaryKey: x => x.{p.ForeignKeyType.KeyProperty.Name},
            List: (int? skip, int? take, string[]? orderBy, CancellationToken ct) => {p.ListMethod!.Name.ToCamelCase()}.List(skip, take, orderBy, ct),
            SetForeignKey: null,
            AfterSaveAction: null,
            AfterCancelAction: null,
            Create: {p.ListMethod.Name.ToCamelCase()}.Create,
            Read: {p.ListMethod.Name.ToCamelCase()}.Read,
            Update: {p.ListMethod.Name.ToCamelCase()}.Update,
            Delete: {p.ListMethod.Name.ToCamelCase()}.Delete
        );
        await {p.ForeignKeyType.Name.ToMultiple()}.InitialiseAsync();"))}
    }}

    public async ValueTask DisposeAsync()
    {{
        if (Cts != null)
        {{
            await Cts.CancelAsync();
            Cts.Dispose();
        }}
        if ({entityName} != null) 
            await {entityName}.DisposeAsync();{string.Join("", clients.Select(p => $@"
        if ({p.ForeignKeyType!.Name.ToMultiple()} != null) 
            await {p.ForeignKeyType.Name.ToMultiple()}.DisposeAsync();"))}
    }}
}}";

        Save();
    }
}
