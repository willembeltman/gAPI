using gAPI.AutoComponent.Generators.Components;
using gAPI.CodeGen.Frontend.Configs;
using gAPI.CodeGen.Frontend.Contexts;
using gAPI.CodeGen.Frontend.Generators;
using gAPI.CodeGen.Frontend.Generators.Razor;
using gAPI.CodeGen.Frontend.Generators.Razor.Pages.Entity;
using gAPI.CodeGen.Frontend.Models.CrudlsModels;

namespace gAPI.CodeGen.Frontend
{
    public class CrudlGenerator
    {
        public CrudlGenerator(
            CrudlType crudl,
            FrontendConfig config,
            ServiceContext serviceContext,
            ImportsGenerator imports)
        {
            CrudlType = crudl;
            Config = config;
            Imports = imports;
            ServiceContext = serviceContext;

            var directoryFullName = Config!.ComponentsDirectory!.FullName;
            var @namespace = Config.ComponentsNamespace!;

            FormGenerator = new FormGenerator(
                    crudl,
                    ServiceContext.ItemDataSource,
                    ServiceContext.ListDataSource,
                    ServiceContext.IClientAuthenticationService,
                    ServiceContext.FormFile,
                    ServiceContext.ToFormFileAsyncExtention,
                    Imports,
                    directoryFullName,
                    @namespace);
            DetailsGenerator = new DetailsGenerator(
                    crudl,
                    ServiceContext.ItemDataSource,
                    Imports,
                    directoryFullName,
                    @namespace);
            ListGenerator = new ListGenerator(
                    crudl,
                    ServiceContext.ItemDataSource,
                    ServiceContext.ListDataSource,
                    ServiceContext.BaseListResponseT,
                    Imports,
                    directoryFullName,
                    @namespace);
            DropDownGenerator = new DropDownGenerator(
                    crudl,
                    ServiceContext.ListDataSource,
                    Imports,
                    directoryFullName,
                    @namespace);
            GridEditGenerator = new GridEditGenerator(
                    crudl,
                    ServiceContext.ListDataSource,
                    Imports,
                    directoryFullName,
                    @namespace);

            var directory = Config!.PagesDirectory;
            @namespace = Config.PagesNamespace!;

            CreateViewGenerator = new CreateViewGenerator(
                crudl,
                ServiceContext.ItemDataSource,
                ServiceContext.ListDataSource,
                ServiceContext.BaseResponse,
                ServiceContext.BaseResponseT,
                ServiceContext.BaseListResponseT,
                ServiceContext.IClientAuthenticationService,
                FormGenerator,
                ServiceContext.LoaderView,
                ServiceContext.ErrorView,
                ServiceContext.RedirectToLogin,
                Imports,
                directory,
                @namespace);
            EditViewGenerator = new EditViewGenerator(
                crudl,
                ServiceContext.ItemDataSource,
                ServiceContext.ListDataSource,
                ServiceContext.BaseResponse,
                ServiceContext.BaseResponseT,
                ServiceContext.BaseListResponseT,
                ServiceContext.IClientAuthenticationService,
                FormGenerator,
                ServiceContext.LoaderView,
                ServiceContext.ErrorView,
                ServiceContext.RedirectToLogin,
                Imports,
                directory,
                @namespace);
            DeleteViewGenerator = new DeleteViewGenerator(
                crudl,
                ServiceContext.ItemDataSource,
                ServiceContext.ListDataSource,
                ServiceContext.BaseResponse,
                ServiceContext.BaseResponseT,
                ServiceContext.BaseListResponseT,
                ServiceContext.IClientAuthenticationService,
                DetailsGenerator,
                ServiceContext.LoaderView,
                ServiceContext.ErrorView,
                ServiceContext.RedirectToLogin,
                Imports,
                directory,
                @namespace);
            IndexViewGenerator = new IndexViewGenerator(
                crudl,
                ServiceContext.ItemDataSource,
                ServiceContext.ListDataSource,
                ServiceContext.BaseResponse,
                ServiceContext.BaseResponseT,
                ServiceContext.BaseListResponseT,
                ServiceContext.IClientAuthenticationService,
                ListGenerator,
                ServiceContext.LoaderView,
                ServiceContext.ErrorView,
                ServiceContext.RedirectToLogin,
                Imports,
                directory,
                @namespace);
        }

        public CrudlType CrudlType { get; set; }
        public FrontendConfig Config { get; set; }
        public ImportsGenerator Imports { get; set; }
        public ServiceContext ServiceContext { get; }
        public CreateViewGenerator CreateViewGenerator { get; }
        public EditViewGenerator EditViewGenerator { get; }
        public DeleteViewGenerator DeleteViewGenerator { get; }
        public IndexViewGenerator IndexViewGenerator { get; }
        public FormGenerator FormGenerator { get; }
        public DetailsGenerator DetailsGenerator { get; }
        public ListGenerator ListGenerator { get; }
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
        }
    }
}