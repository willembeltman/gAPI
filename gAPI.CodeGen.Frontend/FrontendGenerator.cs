using gAPI.CodeGen.Frontend.Configs;
using gAPI.CodeGen.Frontend.Contexts;
using gAPI.CodeGen.Frontend.Generators.Razor;
using gAPI.CodeGen.Frontend.Generators.Razor.Layout;
using gAPI.CodeGen.Frontend.Generators.Razor.Pages;

namespace gAPI.CodeGen.Frontend;

public class FrontendGenerator
{
    public FrontendGenerator(
        FrontendConfig clientConfig)
    {
        Config = clientConfig;
        ServiceContext = new ServiceContext(clientConfig);
        CrudlContext = new CrudlContext(ServiceContext);
        Imports = new ImportsGenerator(clientConfig);

        ErrorView = new ErrorViewGenerator(this);
        LoaderView = new LoaderViewGenerator(this);
        RedirectToHome = new RedirectToHomeGenerator(this);
        RedirectToLogin = new RedirectToLoginGenerator(this);
        NavMenuAuthenticated = new NavMenuAuthenticatedGenerator(this);
        NavMenuNotAuthenticated = new NavMenuNotAuthenticatedGenerator(this);

        PageGenerators = CrudlContext.AllPageMethods
            .Select(pageMethod => new PageGenerator(pageMethod, clientConfig, Imports))
            .ToArray();

        CrudlGenerators = CrudlContext.Types
            .Select(crudl => new CrudlGenerator(crudl, Config, ServiceContext, Imports))
            .ToArray();

        //var directory = Config!.ComponentsDirectory!.FullName;
        //var @namespace = Config.ComponentsNamespace!;
        //CreateViewGenerators = CrudlContext.Types
        //    .Select(crudl => new CreateViewGenerator(crudl, clientConfig, Imports))
        //    .ToArray();
        //EditViewGenerators = CrudlContext.Types
        //    .Select(crudl => new EditViewGenerator(crudl, clientConfig, Imports))
        //    .ToArray();
        //DeleteViewGenerators = CrudlContext.Types
        //    .Select(crudl => new DeleteViewGenerator(crudl, clientConfig, Imports))
        //    .ToArray();
        //IndexViewGenerators = CrudlContext.Types
        //    .Select(crudl => new IndexViewGenerator(crudl, clientConfig, Imports))
        //    .ToArray();
        //FormGenerators = CrudlContext.Types
        //    .Select(crudl => new FormGenerator(
        //        crudl,
        //        ServiceContext.IClientAuthenticationService,
        //        ServiceContext.FormFile,
        //        ServiceContext.ToFormFileAsyncExtention,
        //        Imports,
        //        directory,
        //        @namespace))
        //    .ToArray();
        //DetailsGenerators = CrudlContext.Types
        //    .Select(crudl => new DetailsGenerator(
        //        crudl,
        //        Imports,
        //        directory,
        //        @namespace))
        //    .ToArray();
        //ListGenerators = CrudlContext.Types
        //    .Select(crudl => new ListGenerator(
        //        crudl,
        //        ServiceContext.BaseListResponseT,
        //        Imports,
        //        directory,
        //        @namespace))
        //    .ToArray();
        //DropDownGenerators = CrudlContext.Types
        //    .Select(crudl => new DropDownGenerator(
        //        crudl,
        //        ServiceContext.IClientAuthenticationService, 
        //        Imports,
        //        directory,
        //        @namespace))
        //    .ToArray();

    }

    public FrontendConfig Config { get; }
    public ServiceContext ServiceContext { get; }
    public CrudlContext CrudlContext { get; }

    public ErrorViewGenerator ErrorView { get; }
    public LoaderViewGenerator LoaderView { get; }
    public RedirectToHomeGenerator RedirectToHome { get; }
    public RedirectToLoginGenerator RedirectToLogin { get; }
    public NavMenuAuthenticatedGenerator NavMenuAuthenticated { get; }
    public NavMenuNotAuthenticatedGenerator NavMenuNotAuthenticated { get; }

    public CrudlGenerator[] CrudlGenerators { get; }
    public PageGenerator[] PageGenerators { get; }
    public ImportsGenerator Imports { get; }

    //public CreateViewGenerator[] CreateViewGenerators { get; }
    //public DeleteViewGenerator[] DeleteViewGenerators { get; }
    //public EditViewGenerator[] EditViewGenerators { get; }
    //public IndexViewGenerator[] IndexViewGenerators { get; }

    //public FormGenerator[] FormGenerators { get; }
    //public DetailsGenerator[] DetailsGenerators { get; }
    //public ListGenerator[] ListGenerators { get; }
    //public DropDownGenerator[] DropDownGenerators { get; }

    public void Run()
    {
        ErrorView.GenerateCode();
        LoaderView.GenerateCode();
        RedirectToHome.GenerateCode();
        RedirectToLogin.GenerateCode();
        NavMenuAuthenticated.GenerateCode();
        NavMenuNotAuthenticated.GenerateCode();

        foreach (var dto in PageGenerators) dto.GenerateCode();
        foreach (var dto in CrudlGenerators) dto.GenerateCode();

        //foreach (var dto in CreateViewGenerators) dto.GenerateCode();
        //foreach (var dto in EditViewGenerators) dto.GenerateCode();
        //foreach (var dto in DeleteViewGenerators) dto.GenerateCode();
        //foreach (var dto in IndexViewGenerators) dto.GenerateCode();
        //foreach (var dto in FormGenerators)
        //{
        //    dto.GenerateCode();
        //    dto.Save();
        //}
        //foreach (var dto in DetailsGenerators)
        //{
        //    dto.GenerateCode();
        //    dto.Save();
        //}
        //foreach (var dto in ListGenerators)
        //{
        //    dto.GenerateCode();
        //    dto.Save();
        //}
        //foreach (var dto in DropDownGenerators)
        //{
        //    dto.GenerateCode();
        //    dto.Save();
        //}

        Imports.GenerateCode();
    }
}
