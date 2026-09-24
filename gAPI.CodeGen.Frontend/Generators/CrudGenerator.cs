using gAPI.AutoComponent.Generators.Components;
using gAPI.AutoComponent.Interfaces;
using gAPI.CodeGen.Frontend.Generators.Razor;
using gAPI.CodeGen.Frontend.Generators.Razor.Pages.Entity;
using gAPI.CodeGen.Frontend.Models;
using gAPI.CodeGen.Frontend.Models.Configs;
using gAPI.CodeGen.Frontend.Models.CrudsModels;

namespace gAPI.CodeGen.Frontend.Generators;

public class CrudGenerator
{
    public CrudGenerator(
        CrudType crud,
        FrontendConfig config,
        SharedReferences sharedReferences,
        ServiceContext serviceContext,
        ImportsGenerator imports,
        ISharedReference ItemDataSource,
        ISharedReference ListDataSource,
        ISharedReference IClientAuthenticatedHttpClient
        )
    {
        Config = config;

        var directoryFullName = config!.ComponentsDirectory!.FullName;
        var @namespace = config.ComponentsNamespace!;

        FormGenerator = new FormGenerator(
                crud,
                ItemDataSource,
                ListDataSource,
                IClientAuthenticatedHttpClient,
                sharedReferences.FormFile,
                sharedReferences.IsFormFileExtension,
                imports,
                directoryFullName,
                @namespace);
        DetailsGenerator = new DetailsGenerator(
                crud,
                ItemDataSource,
                imports,
                directoryFullName,
                @namespace);
        ListGenerator = new ListGenerator(
                crud,
                ItemDataSource,
                ListDataSource,
                sharedReferences.BaseListResponseT,
                imports,
                directoryFullName,
                @namespace);
        SelectListGenerator = new SelectListGenerator(
                crud,
                ItemDataSource,
                ListDataSource,
                sharedReferences.BaseListResponseT,
                imports,
                directoryFullName,
                @namespace);
        TableListGenerator = new TableListGenerator(
                crud,
                ItemDataSource,
                ListDataSource,
                sharedReferences.BaseListResponseT,
                imports,
                directoryFullName,
                @namespace);
        DropDownGenerator = new DropDownGenerator(
                crud,
                ListDataSource,
                imports,
                directoryFullName,
                @namespace);
        GridEditGenerator = new GridEditGenerator(
                crud,
                ListDataSource,
                imports,
                directoryFullName,
                @namespace);

        var directory = config!.PagesDirectory;
        @namespace = config.PagesNamespace!;

        CreateViewGenerator = new CreateViewGenerator(
            crud,
            config,
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
            crud,
            config,
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
            crud,
            config,
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
            crud,
            config,
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

    public FrontendConfig Config { get; }


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
        CreateViewGenerator.Save(Config.OverwritePages);

        EditViewGenerator.GenerateCode();
        EditViewGenerator.Save(Config.OverwritePages);

        DeleteViewGenerator.GenerateCode();
        DeleteViewGenerator.Save(Config.OverwritePages);

        IndexViewGenerator.GenerateCode();
        IndexViewGenerator.Save(Config.OverwritePages);

        FormGenerator.GenerateCode();
        if (Config.GenerateComponents)
        FormGenerator.Save(Config.OverwriteComponents);

        DetailsGenerator.GenerateCode();
        if (Config.GenerateComponents)
            DetailsGenerator.Save(Config.OverwriteComponents);

        ListGenerator.GenerateCode();
        if (Config.GenerateComponents)
            ListGenerator.Save(Config.OverwriteComponents);

        DropDownGenerator.GenerateCode();
        if (Config.GenerateComponents)
            DropDownGenerator.Save(Config.OverwriteComponents);

        GridEditGenerator.GenerateCode();
        if (Config.GenerateComponents)
            GridEditGenerator.Save(Config.OverwriteComponents);

        SelectListGenerator.GenerateCode();
        if (Config.GenerateComponents)
            SelectListGenerator.Save(Config.OverwriteComponents);

        TableListGenerator.GenerateCode();
        if (Config.GenerateComponents)
            TableListGenerator.Save(Config.OverwriteComponents);
    }
}