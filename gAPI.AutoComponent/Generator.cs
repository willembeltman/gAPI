using gAPI.AutoComponent.Generators.Components;
using gAPI.AutoComponent.Generators.Helpers;
using gAPI.AutoComponent.Interfaces;
using gAPI.AutoComponent.Models;
using Microsoft.CodeAnalysis.Text;
using System.IO;
using System.Linq;
using System.Text;

namespace gAPI.AutoComponent;

public class Generator
{
    public Generator(
        SharedReferences sharedReferences,
        ServiceContext serviceContext,
        CrudContext crudContext,
        Microsoft.CodeAnalysis.SourceProductionContext spc)
    {
        SharedReferences = sharedReferences;
        ServiceContext = serviceContext;
        CrudContext = crudContext;
        Spc = spc;
    }

    public void Generate()
    {
        //ISharedReference? IClientAuthenticatedHttpClient = SharedReferences.IClientAuthenticatedHttpClient;
        //if (IClientAuthenticatedHttpClient == null)
        //{
        //    var generator = new IClientAuthenticatedHttpClientGenerator(
        //        SharedReferences.State,
        //        SharedReferences.StateChangedHandler,
        //        "",
        //        "gAPI.Generated.Authentication");
        //    generator.GenerateCode();
        //    var iClientAuthenticatedHttpClientFullName = Path.Combine(generator.Directory, generator.FileName);
        //    Spc.AddSource(iClientAuthenticatedHttpClientFullName, SourceText.From(generator.Code, Encoding.UTF8));
        //    IClientAuthenticatedHttpClient = generator;
        //}

        //ISharedReference? ClientAuthenticatedHttpClient = SharedReferences.ClientAuthenticatedHttpClient;
        //if (ClientAuthenticatedHttpClient == null)
        //{
        //    var generator = new ClientAuthenticatedHttpClientGenerator(
        //        SharedReferences.State,
        //        IClientAuthenticatedHttpClient!,
        //        SharedReferences.StateChangedHandler,
        //        "",
        //        "gAPI.Generated.Authentication");
        //    generator.GenerateCode();
        //    var clientAuthenticationServiceFullName = Path.Combine(generator.Directory, generator.FileName);
        //    Spc.AddSource(clientAuthenticationServiceFullName, SourceText.From(generator.Code, Encoding.UTF8));
        //    ClientAuthenticatedHttpClient = generator;
        //}

        ISharedReference? ItemDataSource = SharedReferences.ItemDataSource;
        if (ItemDataSource == null)
        {
            var _ItemDataSource = new ItemDataSourceGenerator(
                SharedReferences.BaseResponseT,
                SharedReferences.BaseResponse,
                SharedReferences.IsFormFileExtension,
                "",
                "gAPI.Generated.Helpers");
            _ItemDataSource.GenerateCode();
            var itemDataSourceFullName = Path.Combine(_ItemDataSource.Directory, _ItemDataSource.FileName);
            Spc.AddSource(itemDataSourceFullName, SourceText.From(_ItemDataSource.Code, Encoding.UTF8));
            ItemDataSource = _ItemDataSource;
        }

        ISharedReference? ListDataSource = SharedReferences.ListDataSource;
        if (ListDataSource == null)
        {
            var _ListDataSource = new ListDataSourceGenerator(
                SharedReferences.BaseListResponseT,
                SharedReferences.BaseResponseT,
                SharedReferences.BaseResponse,
                ItemDataSource,
                "",
                "gAPI.Generated.Helpers");
            _ListDataSource.GenerateCode();
            var listDataSourceFullName = Path.Combine(_ListDataSource.Directory, _ListDataSource.FileName);
            Spc.AddSource(listDataSourceFullName, SourceText.From(_ListDataSource.Code, Encoding.UTF8));
            ListDataSource = _ListDataSource;
        }


        //ISharedReference? ErrorView = ServiceContext.ErrorView;
        //if (ErrorView == null)
        //{
        //    var _ErrorView = new ErrorViewGenerator(this);
        //    _ErrorView.GenerateCode();
        //    var listDataSourceFullName = Path.Combine(_ErrorView.Directory, _ErrorView.FileName);
        //    Spc.AddSource(listDataSourceFullName, SourceText.From(_ErrorView.Code, Encoding.UTF8));
        //    ErrorView = _ErrorView;
        //}
        //ISharedReference? LoaderView = ServiceContext.LoaderView;
        //if (LoaderView == null)
        //{
        //    var _LoaderView = new LoaderViewGenerator(this);
        //    _LoaderView.GenerateCode();
        //    var listDataSourceFullName = Path.Combine(_LoaderView.Directory, _LoaderView.FileName);
        //    Spc.AddSource(listDataSourceFullName, SourceText.From(_LoaderView.Code, Encoding.UTF8));
        //    LoaderView = _LoaderView;
        //}
        //ISharedReference? RedirectToHome = ServiceContext.RedirectToHome;
        //if (RedirectToHome == null)
        //{
        //    var _RedirectToHome = new RedirectToHomeGenerator(this);
        //    _RedirectToHome.GenerateCode();
        //    var listDataSourceFullName = Path.Combine(_RedirectToHome.Directory, _RedirectToHome.FileName);
        //    Spc.AddSource(listDataSourceFullName, SourceText.From(_RedirectToHome.Code, Encoding.UTF8));
        //    RedirectToHome = _RedirectToHome;
        //}
        //ISharedReference? RedirectToLogin = ServiceContext.RedirectToLogin;
        //if (RedirectToLogin == null)
        //{
        //    var _RedirectToLogin = new RedirectToLoginGenerator(this);
        //    _RedirectToLogin.GenerateCode();
        //    var listDataSourceFullName = Path.Combine(_RedirectToLogin.Directory, _RedirectToLogin.FileName);
        //    Spc.AddSource(listDataSourceFullName, SourceText.From(_RedirectToLogin.Code, Encoding.UTF8));
        //    RedirectToLogin = _RedirectToLogin;
        //}

        var Forms = CrudContext.Cruds
            .Where(a => a.ResponseType != null)
            .Select(crud => new AutoFormGenerator(
                this,
                crud,
                ItemDataSource,
                ListDataSource,
                SharedReferences.FormFile,
                SharedReferences.IsFormFileExtension,
                "",
                "gAPI.Generated.Components"))
            .ToArray();
        foreach (var form in Forms)
        {
            form.GenerateCode();
            var formFieldsViewFullName = Path.Combine(form.Directory, form.FileName);
            Spc.AddSource(formFieldsViewFullName, SourceText.From(form.Code, Encoding.UTF8));
        }

        var Details = CrudContext.Cruds
            .Where(a => a.ResponseType != null)
            .Select(crud => new AutoDetailsGenerator(
                this,
                ItemDataSource,
                crud))
            .ToArray();
        foreach (var detail in Details)
        {
            detail.GenerateCode();
            var formFieldsViewFullName = Path.Combine(detail.Directory, detail.FileName);
            Spc.AddSource(formFieldsViewFullName, SourceText.From(detail.Code, Encoding.UTF8));
        }

        var Lists = CrudContext.Cruds
            .Where(a => a.ResponseType != null)
            .Select(crud => new AutoListGenerator(
                this,
                crud,
                ItemDataSource,
                ListDataSource,
                "",
                "gAPI.Generated.Components"))
            .ToArray();
        foreach (var list in Lists)
        {
            list.GenerateCode();
            var formFieldsViewFullName = Path.Combine(list.Directory, list.FileName);
            Spc.AddSource(formFieldsViewFullName, SourceText.From(list.Code, Encoding.UTF8));
        }

        var SelectLists = CrudContext.Cruds
            .Where(a => a.ResponseType != null)
            .Select(crud => new AutoSelectListGenerator(
                this,
                crud,
                ItemDataSource,
                ListDataSource,
                "",
                "gAPI.Generated.Components"))
            .ToArray();
        foreach (var list in SelectLists)
        {
            list.GenerateCode();
            var formFieldsViewFullName = Path.Combine(list.Directory, list.FileName);
            Spc.AddSource(formFieldsViewFullName, SourceText.From(list.Code, Encoding.UTF8));
        }

        var Tables = CrudContext.Cruds
            .Where(a => a.ResponseType != null)
            .Select(crud => new AutoTableGenerator(
                this,
                crud,
                ItemDataSource,
                ListDataSource,
                "",
                "gAPI.Generated.Components"))
            .ToArray();
        foreach (var list in Tables)
        {
            list.GenerateCode();
            var formFieldsViewFullName = Path.Combine(list.Directory, list.FileName);
            Spc.AddSource(formFieldsViewFullName, SourceText.From(list.Code, Encoding.UTF8));
        }

        var DropDowns = CrudContext.Cruds
            .Where(a => a.ResponseType != null)
            .Select(crud => new AutoDropDownGenerator(
                this,
                crud,
                ListDataSource,
                "",
                "gAPI.Generated.Components"))
            .ToArray();
        foreach (var dropDown in DropDowns)
        {
            dropDown.GenerateCode();
            var formFieldsViewFullName = Path.Combine(dropDown.Directory, dropDown.FileName);
            Spc.AddSource(formFieldsViewFullName, SourceText.From(dropDown.Code, Encoding.UTF8));
        }

        var GridEdits = CrudContext.Cruds
            .Where(a => a.ResponseType != null)
            .Select(crud => new AutoGridEditGenerator(
                this,
                crud,
                ListDataSource,
                "",
                "gAPI.Generated.Components"))
            .ToArray();
        foreach (var gridEdit in GridEdits)
        {
            gridEdit.GenerateCode();
            var formFieldsViewFullName = Path.Combine(gridEdit.Directory, gridEdit.FileName);
            Spc.AddSource(formFieldsViewFullName, SourceText.From(gridEdit.Code, Encoding.UTF8));
        }
    }

    public ServiceContext ServiceContext { get; }
    public SharedReferences SharedReferences { get; }
    public CrudContext CrudContext { get; }
    public Microsoft.CodeAnalysis.SourceProductionContext Spc { get; }
}
