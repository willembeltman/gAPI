using gAPI.CodeGen.Frontend.Generators;
using gAPI.CodeGen.Frontend.Generators.Razor;
using gAPI.CodeGen.Frontend.Generators.Razor.Components;
using gAPI.CodeGen.Frontend.Generators.Razor.Layout;
using gAPI.CodeGen.Frontend.Generators.Razor.Pages.Page;
using gAPI.CodeGen.Frontend.Models;
using gAPI.CodeGen.Frontend.Models.Configs;

namespace gAPI.CodeGen.Frontend;

public class FrontendGenerator
{
    public FrontendGenerator(
        FrontendConfig clientConfig)
    {
        Config = clientConfig;
        SharedReferences = new SharedReferences(clientConfig);
        ServiceContext = new ServiceContext(clientConfig);
        CrudlContext = new CrudlContext(ServiceContext, SharedReferences);
        Imports = new ImportsGenerator(clientConfig);

        ErrorView = new ErrorViewGenerator(this);
        LoaderView = new LoaderViewGenerator(this);
        RedirectToHome = new RedirectToHomeGenerator(this);
        RedirectToLogin = new RedirectToLoginGenerator(this);

        //var directory = Config.HelpersDirectory!.FullName;
        //var @namespace = Config.HelpersNamespace!;

        //ItemDataSource = new ItemDataSourceGenerator(
        //    SharedReferences.BaseResponseT,
        //    SharedReferences.BaseResponse,
        //    SharedReferences.IsFormFileExtension,
        //    directory, @namespace)
        //{
        //    FileName = "ItemDataSource.cs"
        //};
        //ListDataSource = new ListDataSourceGenerator(
        //    SharedReferences.BaseListResponseT,
        //    SharedReferences.BaseResponseT,
        //    SharedReferences.BaseResponse,
        //    ItemDataSource,
        //    directory, @namespace)
        //{
        //    FileName = "ListDataSource.cs"
        //};

        //directory = Config.AuthenticationDirectory!.FullName;
        //@namespace = Config.AuthenticationNamespace!;

        //IClientAuthenticatedHttpClient = new IClientAuthenticatedHttpClientGenerator(
        //    SharedReferences.State,
        //    SharedReferences.StateChangedHandler,
        //    directory, @namespace)
        //{
        //    FileName = "IClientAuthenticatedHttpClient.cs"
        //};
        //ClientAuthenticatedHttpClient = new ClientAuthenticatedHttpClientGenerator(
        //    SharedReferences.State,
        //    IClientAuthenticatedHttpClient,
        //    SharedReferences.StateChangedHandler,
        //    directory, @namespace)
        //{
        //    FileName = "ClientAuthenticatedHttpClient.cs"
        //};

        var pages = CrudlContext.AllPageMethods
            .Select(pageMethod => new PageGenerator(pageMethod, clientConfig, Imports))
            .ToArray();

        Pages = [.. pages.Where(a => a.RoutePath != "/")];

        RootPages = [.. pages.Where(a => a.RoutePath == "/")];

        PageIndexes = [.. Pages
            .GroupBy(a => a.RoutePath)
            .Select(a => new IndexGenerator(a.Key, [.. a], clientConfig, Imports))];

        Crudls = [.. CrudlContext.Types
            .Select(crudl => new CrudlGenerator(
                crudl,
                Config,
                SharedReferences,
                ServiceContext,
                Imports,
                SharedReferences.ItemDataSource,
                SharedReferences.ListDataSource,
                SharedReferences.IClientAuthenticatedHttpClient))];

        NavMenuAuthenticated = new NavMenuAuthenticatedGenerator(this);
        NavMenuNotAuthenticated = new NavMenuNotAuthenticatedGenerator(this);
    }

    public FrontendConfig Config { get; }
    public SharedReferences SharedReferences { get; }
    public ServiceContext ServiceContext { get; }
    public CrudlContext CrudlContext { get; }

    public ErrorViewGenerator ErrorView { get; }
    public LoaderViewGenerator LoaderView { get; }
    public RedirectToHomeGenerator RedirectToHome { get; }
    public RedirectToLoginGenerator RedirectToLogin { get; }
    public NavMenuAuthenticatedGenerator NavMenuAuthenticated { get; }
    public NavMenuNotAuthenticatedGenerator NavMenuNotAuthenticated { get; }

    //public ItemDataSourceGenerator ItemDataSource { get; }
    //public ListDataSourceGenerator ListDataSource { get; }
    //public IClientAuthenticatedHttpClientGenerator IClientAuthenticatedHttpClient { get; }
    //public ClientAuthenticatedHttpClientGenerator ClientAuthenticatedHttpClient { get; }

    public CrudlGenerator[] Crudls { get; }
    public PageGenerator[] Pages { get; }
    public PageGenerator[] RootPages { get; }
    public IndexGenerator[] PageIndexes { get; }
    public ImportsGenerator Imports { get; }

    public void Run()
    {
        ErrorView.GenerateCode();
        LoaderView.GenerateCode();
        RedirectToHome.GenerateCode();
        RedirectToLogin.GenerateCode();
        NavMenuAuthenticated.GenerateCode();
        NavMenuNotAuthenticated.GenerateCode();

        foreach (var page in Pages) page.GenerateCode();
        foreach (var page in RootPages) page.GenerateCode();
        foreach (var pageIndex in PageIndexes) pageIndex.GenerateCode();
        foreach (var crudl in Crudls) crudl.GenerateCode();

        //ItemDataSource.GenerateCode();
        //ItemDataSource.Save();

        //ListDataSource.GenerateCode();
        //ListDataSource.Save();

        //IClientAuthenticatedHttpClient.GenerateCode();
        //IClientAuthenticatedHttpClient.Save();

        //ClientAuthenticatedHttpClient.GenerateCode();
        //ClientAuthenticatedHttpClient.Save();

        Imports.GenerateCode();
    }
}
