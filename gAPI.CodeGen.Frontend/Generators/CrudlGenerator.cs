using gAPI.AutoComponent.Generators.Components;
using gAPI.AutoComponent.Interfaces;
using gAPI.CodeGen.Frontend.Generators.Razor;
using gAPI.CodeGen.Frontend.Generators.Razor.Pages.Entity;
using gAPI.CodeGen.Frontend.Models;
using gAPI.CodeGen.Frontend.Models.Configs;
using gAPI.CodeGen.Frontend.Models.CrudlsModels;

namespace gAPI.CodeGen.Frontend.Generators;

public class CrudlGenerator
{
    public CrudlGenerator(
        CrudlType crudl,
        FrontendConfig config,
        SharedReferences sharedReferences,
        ServiceContext serviceContext,
        ImportsGenerator imports,
        ISharedReference ItemDataSource,
        ISharedReference ListDataSource,
        ISharedReference IClientAuthenticatedHttpClient
        )
    {
        var directoryFullName = config!.ComponentsDirectory!.FullName;
        var @namespace = config.ComponentsNamespace!;

        FormGenerator = new FormGenerator(
                crudl,
                ItemDataSource,
                ListDataSource,
                IClientAuthenticatedHttpClient,
                sharedReferences.FormFile,
                sharedReferences.IsFormFileExtension,
                imports,
                directoryFullName,
                @namespace);
        DetailsGenerator = new DetailsGenerator(
                crudl,
                ItemDataSource,
                imports,
                directoryFullName,
                @namespace);
        ListGenerator = new ListGenerator(
                crudl,
                ItemDataSource,
                ListDataSource,
                sharedReferences.BaseListResponseT,
                imports,
                directoryFullName,
                @namespace);
        SelectListGenerator = new SelectListGenerator(
                crudl,
                ItemDataSource,
                ListDataSource,
                sharedReferences.BaseListResponseT,
                imports,
                directoryFullName,
                @namespace);
        TableListGenerator = new TableListGenerator(
                crudl,
                ItemDataSource,
                ListDataSource,
                sharedReferences.BaseListResponseT,
                imports,
                directoryFullName,
                @namespace);
        DropDownGenerator = new DropDownGenerator(
                crudl,
                ListDataSource,
                imports,
                directoryFullName,
                @namespace);
        GridEditGenerator = new GridEditGenerator(
                crudl,
                ListDataSource,
                imports,
                directoryFullName,
                @namespace);

        var directory = config!.PagesDirectory;
        @namespace = config.PagesNamespace!;

        CreateViewGenerator = new CreateViewGenerator(
            crudl,
            ItemDataSource,
            ListDataSource,
            sharedReferences.BaseResponse,
            sharedReferences.BaseResponseT,
            sharedReferences.BaseListResponseT,
            IClientAuthenticatedHttpClient,
            FormGenerator,
            sharedReferences.LoaderView,
            sharedReferences.ErrorView,
            sharedReferences.RedirectToLogin,
            imports,
            directory,
            @namespace);
        EditViewGenerator = new EditViewGenerator(
            crudl,
            ItemDataSource,
            ListDataSource,
            sharedReferences.BaseResponse,
            sharedReferences.BaseResponseT,
            sharedReferences.BaseListResponseT,
            IClientAuthenticatedHttpClient,
            FormGenerator,
            sharedReferences.LoaderView,
            sharedReferences.ErrorView,
            sharedReferences.RedirectToLogin,
            imports,
            directory,
            @namespace);
        DeleteViewGenerator = new DeleteViewGenerator(
            crudl,
            ItemDataSource,
            ListDataSource,
            sharedReferences.BaseResponse,
            sharedReferences.BaseResponseT,
            sharedReferences.BaseListResponseT,
            IClientAuthenticatedHttpClient,
            DetailsGenerator,
            sharedReferences.LoaderView,
            sharedReferences.ErrorView,
            sharedReferences.RedirectToLogin,
            imports,
            directory,
            @namespace);
        IndexViewGenerator = new IndexViewGenerator(
            crudl,
            ItemDataSource,
            ListDataSource,
            sharedReferences.BaseResponse,
            sharedReferences.BaseResponseT,
            sharedReferences.BaseListResponseT,
            IClientAuthenticatedHttpClient,
            ListGenerator,
            sharedReferences.LoaderView,
            sharedReferences.ErrorView,
            sharedReferences.RedirectToLogin,
            imports,
            directory,
            @namespace);
    }

    public CreateViewGenerator CreateViewGenerator { get; }
    public EditViewGenerator EditViewGenerator { get; }
    public DeleteViewGenerator DeleteViewGenerator { get; }
    public IndexViewGenerator IndexViewGenerator { get; }
    public FormGenerator FormGenerator { get; }
    public DetailsGenerator DetailsGenerator { get; }
    public ListGenerator ListGenerator { get; }
    public SelectListGenerator SelectListGenerator { get; }
    public TableListGenerator TableListGenerator { get; }
    public DropDownGenerator DropDownGenerator { get; }
    public GridEditGenerator GridEditGenerator { get; }

    public void GenerateCode()
    {
        CreateViewGenerator.GenerateCode();
        EditViewGenerator.GenerateCode();
        DeleteViewGenerator.GenerateCode();
        IndexViewGenerator.GenerateCode();

        FormGenerator.GenerateCode();
        FormGenerator.Save();

        DetailsGenerator.GenerateCode();
        DetailsGenerator.Save();

        ListGenerator.GenerateCode();
        ListGenerator.Save();

        DropDownGenerator.GenerateCode();
        DropDownGenerator.Save();

        GridEditGenerator.GenerateCode();
        GridEditGenerator.Save();

        SelectListGenerator.GenerateCode();
        SelectListGenerator.Save();

        TableListGenerator.GenerateCode();
        TableListGenerator.Save();
    }
}