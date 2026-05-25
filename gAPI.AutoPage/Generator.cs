using gAPI.AutoPage.Generators.Components;
using gAPI.AutoPage.Generators.Helpers;
using gAPI.AutoPage.Generators.Layout;
using gAPI.AutoPage.Generators.Pages;
using gAPI.AutoPage.Interfaces;
using gAPI.AutoPage.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.IO;
using System.Linq;
using System.Text;

namespace gAPI.AutoPage;

public class Generator : IContext
{
    public Generator(
        SharedReferences sharedReferences,
        ServiceContext serviceContext,
        CrudContext crudContext,
        SourceProductionContext spc)
    {
        SharedReferences = sharedReferences;
        ServiceContext = serviceContext;
        CrudContext = crudContext;
        Spc = spc;

        Components = CrudContext.ComponentMethods
            .Select(page => new AutoComponentGenerator(this, page))
            .ToArray();

        var pageMethods = CrudContext.PageMethods
            .Select(page => new AutoPageGenerator(this, page))
            .ToArray();

        Pages = [.. pageMethods.Where(a => a.RoutePath != "/")];

        RootPages = [.. pageMethods.Where(a => a.RoutePath == "/")];

        PageIndexes = [.. Pages
            .GroupBy(a => a.RoutePath)
            .Select(a => new AutoIndexGenerator(this, a.Key, [.. a]))];

        NavMenuAuthenticated = new AutoNavMenuAuthenticatedGenerator(this);
        NavMenuNotAuthenticated = new AutoNavMenuNotAuthenticatedGenerator(this);

        ItemDataSource = new ItemDataSourceGenerator(this);
        ListDataSource = new ListDataSourceGenerator(this);
    }

    public void Generate()
    {
        foreach (var component in Components)
        {
            component.GenerateCode();
            var componentFullName = Path.Combine(component.Directory, component.FileName);
            Spc.AddSource(componentFullName, SourceText.From(component.Code, Encoding.UTF8));
        }
        foreach (var page in Pages)
        {
            page.GenerateCode();
            var pageFullName = Path.Combine(page.Directory, page.FileName);
            Spc.AddSource(pageFullName, SourceText.From(page.Code, Encoding.UTF8));
        }
        foreach (var page in RootPages)
        {
            page.GenerateCode();
            var pageFullName = Path.Combine(page.Directory, page.FileName);
            Spc.AddSource(pageFullName, SourceText.From(page.Code, Encoding.UTF8));
        }
        foreach (var pageIndex in PageIndexes)
        {
            pageIndex.GenerateCode();
            var pageIndexFullName = Path.Combine(pageIndex.Directory, pageIndex.FileName);
            Spc.AddSource(pageIndexFullName, SourceText.From(pageIndex.Code, Encoding.UTF8));
        }

        NavMenuAuthenticated.GenerateCode();
        var navMenuAuthenticatedFullName = Path.Combine(NavMenuAuthenticated.Directory, NavMenuAuthenticated.FileName);
        Spc.AddSource(navMenuAuthenticatedFullName, SourceText.From(NavMenuAuthenticated.Code, Encoding.UTF8));

        NavMenuNotAuthenticated.GenerateCode();
        var navMenuNotAuthenticatedFullName = Path.Combine(NavMenuNotAuthenticated.Directory, NavMenuNotAuthenticated.FileName);
        Spc.AddSource(navMenuNotAuthenticatedFullName, SourceText.From(NavMenuNotAuthenticated.Code, Encoding.UTF8));

    }

    public ServiceContext ServiceContext { get; }
    public SharedReferences SharedReferences { get; }
    public CrudContext CrudContext { get; }
    public Microsoft.CodeAnalysis.SourceProductionContext Spc { get; }

    public AutoComponentGenerator[] Components { get; }
    public AutoPageGenerator[] Pages { get; private set; }
    public AutoPageGenerator[] RootPages { get; private set; }
    public AutoIndexGenerator[] PageIndexes { get; private set; }
    public AutoNavMenuAuthenticatedGenerator NavMenuAuthenticated { get; }
    public AutoNavMenuNotAuthenticatedGenerator NavMenuNotAuthenticated { get; private set; }
    public ItemDataSourceGenerator ItemDataSource { get; }
    public ListDataSourceGenerator ListDataSource { get; }

    ICrudType[] IContext.Cruds => CrudContext.Cruds;
    IPageIndex[] IContext.PageIndexes => PageIndexes;
    IPage[] IContext.RootPages => RootPages;
    ISharedReferences IContext.SharedReferences => SharedReferences;
    ISharedReference IContext.ListDataSource => ItemDataSource;
}
