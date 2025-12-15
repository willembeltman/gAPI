using gAPI.AutoComponents.Interfaces;
using gAPI.CodeGen.Frontend.Helpers;
using gAPI.CodeGen.Frontend.Models.CrudlsModels;

namespace gAPI.CodeGen.Frontend.Generators.Razor.Pages.Entity;

public class EditViewGenerator : BaseGenerator
{
    public EditViewGenerator(
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
        Name = "Edit";

        FileName = $"{crudlType.Name!.ToMultiple()}\\{Name}.razor";
    }

    public CrudlType CrudlType { get; }
    public ISharedReference ItemDataSource { get; }
    public ISharedReference ListDataSource { get; }
    public ISharedReference FormView { get; }
    public ISharedReference BaseResponse { get; }
    public ISharedReference BaseResponseT { get; }
    public ISharedReference BaseListResponseT { get; }
    public ISharedReference IClientAuthenticationService { get; }
    public ISharedReference ErrorView { get; }
    public ISharedReference RedirectToLoginView { get; }
    public ISharedReference LoaderView { get; }
    public ImportsGenerator Imports { get; }

    public void GenerateCode()
    {
        if (CrudlType.UpdateMethod == null ||
            CrudlType.ReadMethod == null ||
            CrudlType.Name == null)
            return;

        Imports.Reg(CrudlType);
        Imports.Reg(ItemDataSource);
        Imports.Reg(ListDataSource);
        Imports.Reg(LoaderView);
        Imports.Reg(FormView);
        Imports.Reg(ErrorView);
        Imports.Reg(RedirectToLoginView);
        Imports.Reg(IClientAuthenticationService);
        Imports.Reg("Microsoft.AspNetCore.Components");
        Imports.Reg("Microsoft.AspNetCore.Components.Forms");
        Imports.Reg("Microsoft.JSInterop");

        var entityName = CrudlType.Name;
        var keyType = CrudlType.KeyProperty.TypeSimpleName;
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

        var clients = CrudlType.ForeignItemProperties
            .Where(p => !p.IsStateManaged || CrudlType.IsUser)
            .ToArray();

        foreach (var p in clients)
            Imports.Reg(p.ListMethod!.Client!.Interface.Namespace);

        var asyncHeader = clients.Length == 0 ? @"
" : $@" 
@implements IAsyncDisposable
{string.Join("", clients.Select(p => $@"@inject {p.ListMethod!.Client.Interface.Name} {p.ListMethod.Client.Name}
"))}";
        var asyncFoorter = clients.Length == 0 ? "" : $@"

    public async ValueTask DisposeAsync()
    {{{string.Join("", clients.Select(p => $@"
        if ({p.ForeignKeyType!.Name.ToMultiple()} != null) await {p.ForeignKeyType.Name.ToMultiple()}.DisposeAsync();"))}
    }}";

        Code = $@"@page ""/{entityName.ToLower().ToMultiple()}/edit/{{id{paramRouteType}}}""{asyncHeader}@inject {CrudlType.UpdateMethod.Client!.Interface.Name} {CrudlType.UpdateMethod.Client.Name}
@inject {IClientAuthenticationService.Name} ClientAuthenticationService
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
                <{FormView.Name} DataSource=""{entityName}""{string.Join("", clients.Select(p => $@" {p.ForeignKeyType!.Name.ToMultiple()}=""{p.ForeignKeyType.Name.ToMultiple()}"""))} />
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

    private {ItemDataSource.Name}<{entityName}, {keyType}>? {entityName};{string.Join("", clients.Select(p => @$"
    private {ListDataSource.Name}<{p.ForeignKeyType!.Name}, {p.ForeignKeyType.KeyProperty!.TypeSimpleName}>? {p.ForeignKeyType.Name.ToMultiple()};"))}

    protected override async Task OnInitializedAsync()
    {{
        if (await ClientAuthenticationService.IsAuthenticated() == false || id == null)
        {{
            return;
        }}

        {entityName} = new {ItemDataSource.Name}<{entityName}, {CrudlType.KeyProperty.TypeSimpleName}>(
            GetPrimaryKey: e => e.{CrudlType.KeyProperty.Name},
            AfterSaveAction: e => NavigationManager.NavigateTo(""/{entityName.ToLower().ToMultiple()}""),
            AfterCancelAction: e => NavigationManager.NavigateTo(""/{entityName.ToLower().ToMultiple()}""),
            Create: {CrudlType.UpdateMethod.Client.Name}.Create,
            Read: {CrudlType.UpdateMethod.Client.Name}.Read,
            Update: {CrudlType.UpdateMethod.Client.Name}.Update,
            Delete: {CrudlType.UpdateMethod.Client.Name}.Delete,
            FileUpdate: {(CrudlType.IsStorageFile ? $"{CrudlType.UpdateMethod.Client.Name}.FileUpdate" : "null")},
            FileDelete: {(CrudlType.IsStorageFile ? $"{CrudlType.UpdateMethod.Client.Name}.FileDelete" : "null")}
        );
        await {entityName}.LoadModelAsync({idGetter});{string.Join("", clients.Select(p => $@"

        {p.ForeignKeyType!.Name.ToMultiple()} = new {ListDataSource.Name}<{p.ForeignKeyType.Name}, {p.ForeignKeyType.KeyProperty!.TypeSimpleName}>(
            JS,
            StateHasChanged,
            GetPrimaryKey: x => x.{p.ForeignKeyType.KeyProperty.Name},
            List: (int? skip, int? take, string[]? orderBy) => {p.ListMethod!.Client!.Name}.List(skip, take, orderBy),
            SetForeignKey: null,
            AfterSaveAction: null,
            AfterCancelAction: null,
            Create: {p.ListMethod.Client.Name}.Create,
            Read: {p.ListMethod.Client.Name}.Read,
            Update: {p.ListMethod.Client.Name}.Update,
            Delete: {p.ListMethod.Client.Name}.Delete
        );
        await {p.ForeignKeyType.Name.ToMultiple()}.InitialiseAsync();"))}
    }}{asyncFoorter}
}}";

        Save();
    }
}
