using gAPI.AutoComponent.Generators.Commen;
using gAPI.AutoComponent.Generators.Components;
using gAPI.AutoComponent.Generators.Helpers;
using gAPI.AutoComponent.Interfaces;
using gAPI.AutoComponent.Models;
using gAPI.AutoComponent.Models.Configs;
using Microsoft.CodeAnalysis.Text;
using System.IO;
using System.Linq;
using System.Text;

namespace gAPI.AutoComponent;

public class Generator
{
    public Generator(
        ComponentConfig config,
        SharedReferences sharedReferences,
        ServiceContext serviceContext,
        CrudlContext crudlContext,
        Microsoft.CodeAnalysis.SourceProductionContext spc)
    {
        Config = config;
        SharedReferences = sharedReferences;
        ServiceContext = serviceContext;
        CrudlContext = crudlContext;
        Spc = spc;
    }

    public void GenerateViews()
    {
        ISharedReference? IClientAuthenticationService = SharedReferences.IClientAuthenticationService;
        if (IClientAuthenticationService == null)
        { 
            var generator = new IClientAuthenticationServiceGenerator(
                SharedReferences.State,
                SharedReferences.StateChangedHandler,
                Config.Authentication_Destination.Directory,
                Config.Authentication_Destination.Namespace);
            generator.GenerateCode();
            var iClientAuthenticationServiceFullName = Path.Combine(generator.Directory, generator.FileName);
            Spc.AddSource(iClientAuthenticationServiceFullName, SourceText.From(generator.Code, Encoding.UTF8));
            IClientAuthenticationService = generator;
        }

        ISharedReference? ClientAuthenticationService = SharedReferences.ClientAuthenticationService;
        if (ClientAuthenticationService == null)
        {
            var generator = new ClientAuthenticationServiceGenerator(
                SharedReferences.State,
                IClientAuthenticationService!,
                SharedReferences.StateChangedHandler,
                Config.Authentication_Destination.Directory,
                Config.Authentication_Destination.Namespace);
            generator.GenerateCode();
            var clientAuthenticationServiceFullName = Path.Combine(generator.Directory, generator.FileName);
            Spc.AddSource(clientAuthenticationServiceFullName, SourceText.From(generator.Code, Encoding.UTF8));
            ClientAuthenticationService = generator;
        }

        ISharedReference? ItemDataSource = SharedReferences.ItemDataSource;
        if (ItemDataSource == null)
        {
            var _ItemDataSource = new ItemDataSourceGenerator(
                SharedReferences.BaseResponseT,
                SharedReferences.BaseResponse,
                SharedReferences.IsFormFileExtention,
                Config.Helpers_Destination.Directory,
                Config.Helpers_Destination.Namespace);
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
                Config.Helpers_Destination.Directory,
                Config.Helpers_Destination.Namespace);
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

        var Forms = CrudlContext.Crudls
            .Where(a => a.Dto != null)
            .Select(crudl => new AutoFormGenerator(
                this,
                crudl,
                ItemDataSource,
                ListDataSource,
                SharedReferences.FormFile,
                SharedReferences.IsFormFileExtention,
                Config.Components_Destination.Directory,
                Config.Components_Destination.Namespace))
            .ToArray();
        foreach (var form in Forms)
        {
            form.GenerateCode();
            var formFieldsViewFullName = Path.Combine(form.Directory, form.FileName);
            Spc.AddSource(formFieldsViewFullName, SourceText.From(form.Code, Encoding.UTF8));
        }

        var Details = CrudlContext.Crudls
            .Where(a => a.Dto != null)
            .Select(crudl => new AutoDetailsGenerator(
                this,
                ItemDataSource,
                crudl))
            .ToArray();
        foreach (var detail in Details)
        {
            detail.GenerateCode();
            var formFieldsViewFullName = Path.Combine(detail.Directory, detail.FileName);
            Spc.AddSource(formFieldsViewFullName, SourceText.From(detail.Code, Encoding.UTF8));
        }

        var Lists = CrudlContext.Crudls
            .Where(a => a.Dto != null)
            .Select(crudl => new AutoListGenerator(
                this,
                crudl,
                ItemDataSource,
                ListDataSource,
                Config.Components_Destination.Directory,
                Config.Components_Destination.Namespace))
            .ToArray();
        foreach (var list in Lists)
        {
            list.GenerateCode();
            var formFieldsViewFullName = Path.Combine(list.Directory, list.FileName);
            Spc.AddSource(formFieldsViewFullName, SourceText.From(list.Code, Encoding.UTF8));
        }

        var SelectLists = CrudlContext.Crudls
            .Where(a => a.Dto != null)
            .Select(crudl => new AutoSelectListGenerator(
                this,
                crudl,
                ItemDataSource,
                ListDataSource,
                Config.Components_Destination.Directory,
                Config.Components_Destination.Namespace))
            .ToArray();
        foreach (var list in SelectLists)
        {
            list.GenerateCode();
            var formFieldsViewFullName = Path.Combine(list.Directory, list.FileName);
            Spc.AddSource(formFieldsViewFullName, SourceText.From(list.Code, Encoding.UTF8));
        }

        var Tables = CrudlContext.Crudls
            .Where(a => a.Dto != null)
            .Select(crudl => new AutoTableGenerator(
                this,
                crudl,
                ItemDataSource,
                ListDataSource,
                Config.Components_Destination.Directory,
                Config.Components_Destination.Namespace))
            .ToArray();
        foreach (var list in Tables)
        {
            list.GenerateCode();
            var formFieldsViewFullName = Path.Combine(list.Directory, list.FileName);
            Spc.AddSource(formFieldsViewFullName, SourceText.From(list.Code, Encoding.UTF8));
        }

        var DropDowns = CrudlContext.Crudls
            .Where(a => a.Dto != null)
            .Select(crudl => new AutoDropDownGenerator(
                this,
                crudl,
                ListDataSource,
                Config.Components_Destination.Directory,
                Config.Components_Destination.Namespace))
            .ToArray();
        foreach (var dropDown in DropDowns)
        {
            dropDown.GenerateCode();
            var formFieldsViewFullName = Path.Combine(dropDown.Directory, dropDown.FileName);
            Spc.AddSource(formFieldsViewFullName, SourceText.From(dropDown.Code, Encoding.UTF8));
        }

        var GridEdits = CrudlContext.Crudls
            .Where(a => a.Dto != null)
            .Select(crudl => new AutoGridEditGenerator(
                this,
                crudl,
                ListDataSource,
                Config.Components_Destination.Directory,
                Config.Components_Destination.Namespace))
            .ToArray();
        foreach (var gridEdit in GridEdits)
        {
            gridEdit.GenerateCode();
            var formFieldsViewFullName = Path.Combine(gridEdit.Directory, gridEdit.FileName);
            Spc.AddSource(formFieldsViewFullName, SourceText.From(gridEdit.Code, Encoding.UTF8));
        }
    }

    public ServiceContext ServiceContext { get; }
    public ComponentConfig Config { get; }
    public SharedReferences SharedReferences { get; }
    public CrudlContext CrudlContext { get; }
    public Microsoft.CodeAnalysis.SourceProductionContext Spc { get; }
}
