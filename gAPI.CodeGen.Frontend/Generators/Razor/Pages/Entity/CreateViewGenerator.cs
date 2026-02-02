using gAPI.AutoComponent.Interfaces;
using gAPI.CodeGen.Frontend.Helpers;
using gAPI.CodeGen.Frontend.Models.CrudlsModels;

namespace gAPI.CodeGen.Frontend.Generators.Razor.Pages.Entity;

public class CreateViewGenerator : BaseGenerator
{
    public CreateViewGenerator(
        CrudlType crudlType,
        ISharedReference itemDataSource,
        ISharedReference listDataSource,
        ISharedReference baseResponse,
        ISharedReference baseResponseT,
        ISharedReference baseListResponseT,
        ISharedReference iClientAuthenticationService,
        ISharedReference formView,
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
        BaseResponse = baseResponse;
        BaseResponseT = baseResponseT;
        BaseListResponseT = baseListResponseT;
        IClientAuthenticationService = iClientAuthenticationService;
        FormView = formView;
        LoaderView = loaderView;
        ErrorView = errorView;
        RedirectToLoginView = redirectToLoginView;
        Imports = imports;

        Directory = directory;
        Namespace = $"{@namespace}.{crudlType.Name!.ToMultiple()}";
        Name = "Create";

        FileName = $"{crudlType.Name!.ToMultiple()}\\{Name}.razor";
    }

    public CrudlType CrudlType { get; }
    public ISharedReference ItemDataSource { get; }
    public ISharedReference ListDataSource { get; }
    public ISharedReference BaseResponse { get; }
    public ISharedReference BaseResponseT { get; }
    public ISharedReference BaseListResponseT { get; }
    public ISharedReference IClientAuthenticationService { get; }
    public ISharedReference FormView { get; }
    public ISharedReference LoaderView { get; }
    public ISharedReference ErrorView { get; }
    public ISharedReference RedirectToLoginView { get; }
    public ImportsGenerator Imports { get; }

    public void GenerateCode()
    {
        if (CrudlType.CreateMethod == null || CrudlType.Name == null)
            return;

        Imports.Reg(CrudlType);
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
        Imports.Reg(IClientAuthenticationService);
        Imports.Reg(CrudlType.CreateMethod.Client);
        Imports.Reg(CrudlType.CreateMethod.Client?.Interface);

        var clients = CrudlType.ForeignItemProperties
            .Where(p => !p.IsStateManaged && !p.IsReadOnly)
            .ToArray();

        foreach (var client in clients)
        {
            Imports.Reg(client.ListMethod!.Client!.Interface.Namespace);
            Imports.Reg(client.ListMethod.Client);
            Imports.Reg(client.ForeignKeyType!);
        }

        var name = CrudlType.Name;
        var namePlural = name.ToMultiple();
        var nameLowerPlural = name.ToLower().ToMultiple();

        Code = $@"@page ""/{nameLowerPlural}/create""
@implements IAsyncDisposable{string.Join("", clients.Select(p => $@"
@inject {p.ListMethod!.Client!.Interface.Name} {p.ListMethod!.Client!.Name}"))}
@inject {CrudlType.CreateMethod.Client!.Interface.Name} {CrudlType.CreateMethod.Client.Name}
@inject {IClientAuthenticationService.Name} ClientAuthenticationService
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
    private {ItemDataSource.Name}<{name}, {CrudlType.KeyProperty!.TypeSimpleName}>? {name};{string.Join("", clients.Select(p => $@"
    private {ListDataSource.Name}<{p.ForeignKeyType!.Name}, {p.ForeignKeyType.KeyProperty!.TypeSimpleName}>? {p.ForeignKeyType!.Name.ToMultiple()};"))}

    protected override async Task OnInitializedAsync()
    {{
        if (Cts != null)
        {{
            await Cts.CancelAsync();
            Cts.Dispose();
        }}

        Cts = new CancellationTokenSource();

        if (await ClientAuthenticationService.IsAuthenticatedAsync(Cts.Token) == false)
            return;

        {name} = new {ItemDataSource.Name}<{name}, {CrudlType.KeyProperty.TypeSimpleName}>(
            GetPrimaryKey: {CrudlType.Name.ToLower()} => {CrudlType.Name.ToLower()}.{CrudlType.KeyProperty.Name},
            AfterSaveAction: {CrudlType.Name.ToLower()} => NavigationManager.NavigateTo(""/{nameLowerPlural}""),
            AfterCancelAction: {CrudlType.Name.ToLower()} => NavigationManager.NavigateTo(""/{nameLowerPlural}""),
            Create: {CrudlType.CreateMethod.Client.Name}.Create,
            Read: null,
            Update: null,
            Delete: null,{(CrudlType.IsStorageFileUrlProperty ? $@"
            FileUpdate: {CrudlType.CreateMethod.Client.Name}.FileUpdate,
            FileDelete: null" : $@"
            FileUpdate: null,
            FileDelete: null")}
        );
        {name}.NewModel();{string.Join("", clients.Select(p => $@"

        {p.ForeignKeyType!.Name.ToMultiple()} = new {ListDataSource.Name}<{p.ForeignKeyType!.Name}, {p.ForeignKeyType.KeyProperty!.TypeSimpleName}>(
            JS,
            StateHasChanged,
            GetPrimaryKey: {p.ForeignKeyType.Name.ToLower()} => {p.ForeignKeyType.Name.ToLower()}.{p.ForeignKeyType.KeyProperty.Name},
            List: {p.ListMethod!.Client!.Name}.List,
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
