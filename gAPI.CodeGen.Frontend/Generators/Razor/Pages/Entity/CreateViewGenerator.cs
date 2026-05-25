using gAPI.AutoComponent.Interfaces;
using gAPI.CodeGen.Frontend.Helpers;
using gAPI.CodeGen.Frontend.Models.CrudsModels;

namespace gAPI.CodeGen.Frontend.Generators.Razor.Pages.Entity;

public class CreateViewGenerator : BaseGenerator
{
    public CreateViewGenerator(
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
        Name = "Create";

        FileName = $"{crudType.Name!.ToMultiple()}\\{Name}.razor";
    }

    public CrudType CrudType { get; }
    public ISharedReference ItemDataSource { get; }
    public ISharedReference ListDataSource { get; }
    public ISharedReference BaseResponse { get; }
    public ISharedReference BaseResponseT { get; }
    public ISharedReference BaseListResponseT { get; }
    public ISharedReference IClientAuthenticatedHttpClient { get; }
    public ISharedReference FormView { get; }
    public ISharedReference LoaderView { get; }
    public ISharedReference ErrorView { get; }
    public ISharedReference RedirectToLoginView { get; }
    public ImportsGenerator Imports { get; }

    public void GenerateCode()
    {
        if (CrudType.CreateMethod == null || CrudType.Name == null)
            return;

        Imports.Reg(CrudType);
        Imports.Reg(ItemDataSource);
        Imports.Reg(ListDataSource);
        Imports.Reg(BaseResponse);
        Imports.Reg(BaseResponseT);
        Imports.Reg(BaseListResponseT);
        Imports.Reg(FormView);
        Imports.Reg(LoaderView);
        Imports.Reg(ErrorView);
        Imports.Reg(RedirectToLoginView);
        Imports.Reg("Microsoft.AspNetCore.Components");
        Imports.Reg("Microsoft.AspNetCore.Components.Authorization");
        Imports.Reg("Microsoft.AspNetCore.Components.Forms");
        Imports.Reg("Microsoft.JSInterop");
        Imports.Reg(IClientAuthenticatedHttpClient);
        Imports.Reg(CrudType.CreateMethod.Interface);

        var clients = CrudType.ForeignItemProperties
            .Where(p => !p.IsStateManaged && !p.IsReadOnly)
            .ToArray();

        foreach (var client in clients)
        {
            Imports.Reg(client.ListMethod!.Interface);
            Imports.Reg(client.ForeignKeyType!);
        }

        var name = CrudType.Name;
        var namePlural = name.ToMultiple();
        var nameLowerPlural = name.ToLower().ToMultiple();

        Code = $@"@page ""/{nameLowerPlural}/create""
@implements IAsyncDisposable{string.Join("", clients.Select(p => $@"
@inject {p.ListMethod!.Interface.Name} {p.ListMethod!.Name.ToCamelCase()}"))}
@inject {CrudType.CreateMethod.Interface.Name} {CrudType.CreateMethod.Name.ToCamelCase()}
@inject {IClientAuthenticatedHttpClient.Name} ClientAuthenticatedHttpClient
@inject IJSRuntime JS
@inject NavigationManager NavigationManager

<PageTitle>Create {name}</PageTitle>

<AuthorizeView>
    <Authorized Context=""_"">
        <h3>Create {name}</h3>
        <{LoaderView.Name} Response=""{name}?.Response"">
            <EditForm Model=""{name}!.Model"" OnValidSubmit=""{name}.HandleValidSubmit"">
                <DataAnnotationsValidator />
                <ValidationSummary />
                <{FormView.Name} DataSource=""{name}""{string.Join("", clients.Select(p => $@" {p.ForeignKeyType!.Name.ToMultiple()}=""{p.ForeignKeyType!.Name.ToMultiple()}"""))} />
                <{ErrorView.Name} Response=""{name}.StatusResponse"" />
                <button id=""create"" class=""btn btn-primary"" type=""submit"">➕ Create</button>
                <button id=""cancel"" class=""btn btn-secondary ms-2"" @onclick=""{name}.Cancel"">Cancel</button>
            </EditForm>
        </{LoaderView.Name}>
    </Authorized>
    <NotAuthorized>
        <{RedirectToLoginView.Name} />
    </NotAuthorized>
</AuthorizeView>

@code {{
    private CancellationTokenSource? Cts;
    private {ItemDataSource.Name}<{name}, {CrudType.KeyProperty!.TypeSimpleName}>? {name};{string.Join("", clients.Select(p => $@"
    private {ListDataSource.Name}<{p.ForeignKeyType!.Name}, {p.ForeignKeyType.KeyProperty!.TypeSimpleName}>? {p.ForeignKeyType!.Name.ToMultiple()};"))}

    protected override async Task OnInitializedAsync()
    {{
        if (Cts != null)
        {{
            await Cts.CancelAsync();
            Cts.Dispose();
        }}

        Cts = new CancellationTokenSource();

        if (await ClientAuthenticatedHttpClient.IsAuthenticatedAsync(Cts.Token) == false)
            return;

        {name} = new {ItemDataSource.Name}<{name}, {CrudType.KeyProperty.TypeSimpleName}>(
            GetPrimaryKey: {CrudType.Name.ToLower()} => {CrudType.Name.ToLower()}.{CrudType.KeyProperty.Name},
            AfterSaveAction: {CrudType.Name.ToLower()} => NavigationManager.NavigateTo(""/{nameLowerPlural}""),
            AfterCancelAction: {CrudType.Name.ToLower()} => NavigationManager.NavigateTo(""/{nameLowerPlural}""),
            Create: {CrudType.CreateMethod.Name.ToCamelCase()}.Create,
            Read: null,
            Update: null,
            Delete: null,{(CrudType.IsStorageFileUrlProperty ? $@"
            FileUpdate: {CrudType.CreateMethod.Name.ToCamelCase()}.FileUpdate,
            FileDelete: null" : $@"
            FileUpdate: null,
            FileDelete: null")}
        );
        {name}.NewModel();{string.Join("", clients.Select(p => $@"

        {p.ForeignKeyType!.Name.ToMultiple()} = new {ListDataSource.Name}<{p.ForeignKeyType!.Name}, {p.ForeignKeyType.KeyProperty!.TypeSimpleName}>(
            JS,
            StateHasChanged,
            GetPrimaryKey: {p.ForeignKeyType.Name.ToLower()} => {p.ForeignKeyType.Name.ToLower()}.{p.ForeignKeyType.KeyProperty.Name},
            List: {p.ListMethod!.Name.ToCamelCase()}.List,
            SetForeignKey: null,
            AfterSaveAction: null,
            AfterCancelAction: null,
            Create: null,
            Read: null,
            Update: null,
            Delete: null
        );
        await {p.ForeignKeyType!.Name.ToMultiple()}.InitialiseAsync();"))}
    }}

    public async ValueTask DisposeAsync()
    {{
        if (Cts != null)
        {{
            await Cts.CancelAsync();
            Cts.Dispose();
        }}
        if ({name} != null) 
            await {name}.DisposeAsync();{string.Join("", clients.Select(p => $@"
        if ({p.ForeignKeyType!.Name.ToMultiple()} != null) 
            await {p.ForeignKeyType!.Name.ToMultiple()}.DisposeAsync();"))}
    }}
}}";

        Save();
    }
}
