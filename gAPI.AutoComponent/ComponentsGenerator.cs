using gAPI.AutoComponent.Configs;
using gAPI.AutoComponent.Generators.Components;
using gAPI.AutoComponent.Generators.Helpers;
using Microsoft.CodeAnalysis.Text;
using System.IO;
using System.Linq;
using System.Text;

namespace gAPI.AutoComponent
{
    public class ComponentsGenerator
    {
        public ComponentsGenerator(
            ServiceContext serviceContext,
            Microsoft.CodeAnalysis.SourceProductionContext spc)
        {
            ServiceContext = serviceContext;
            Config = ServiceContext.Config!;
            CrudlContext = new CrudlContext(ServiceContext);
            Spc = spc;
        }

        public void GenerateViews()
        {
            var StateChangedHandler = new StateChangedHandlerGenerator(
                Config.Authentication_Destination.Directory,
                Config.Authentication_Destination.Namespace);
            StateChangedHandler.GenerateCode();
            var stateChangedHandlerFullName = Path.Combine(StateChangedHandler.Directory, StateChangedHandler.FileName);
            Spc.AddSource(stateChangedHandlerFullName, SourceText.From(StateChangedHandler.Code, Encoding.UTF8));

            //var IClientAuthenticationService = new IClientAuthenticationServiceGenerator(
            //    ServiceContext.State,
            //    StateChangedHandler,
            //    Config.Authentication_Destination.Directory,
            //    Config.Authentication_Destination.Namespace);
            //IClientAuthenticationService.GenerateCode();
            //var iClientAuthenticationServiceFullName = Path.Combine(IClientAuthenticationService.Directory, IClientAuthenticationService.FileName);
            //Spc.AddSource(iClientAuthenticationServiceFullName, SourceText.From(IClientAuthenticationService.Code, Encoding.UTF8));

            //var ClientAuthenticationService = new ClientAuthenticationServiceGenerator(
            //    ServiceContext.BaseResponse,
            //    ServiceContext.BaseResponseT,
            //    ServiceContext.State,
            //    IClientAuthenticationService,
            //    StateChangedHandler,
            //    Config.Authentication_Destination.Directory,
            //    Config.Authentication_Destination.Namespace);
            //ClientAuthenticationService.GenerateCode();
            //var clientAuthenticationServiceFullName = Path.Combine(ClientAuthenticationService.Directory, ClientAuthenticationService.FileName);
            //Spc.AddSource(clientAuthenticationServiceFullName, SourceText.From(ClientAuthenticationService.Code, Encoding.UTF8));

            var ItemDataSource = new ItemDataSourceGenerator(
                ServiceContext.BaseResponseT,
                ServiceContext.BaseResponse,
                ServiceContext.ToFormFileAsyncExtention,
                Config.Helpers_Destination.Directory,
                Config.Helpers_Destination.Namespace);
            ItemDataSource.GenerateCode();
            var itemDataSourceFullName = Path.Combine(ItemDataSource.Directory, ItemDataSource.FileName);
            Spc.AddSource(itemDataSourceFullName, SourceText.From(ItemDataSource.Code, Encoding.UTF8));

            var ListDataSource = new ListDataSourceGenerator(
                ServiceContext.BaseListResponseT,
                ServiceContext.BaseResponseT,
                ServiceContext.BaseResponse,
                ItemDataSource,
                Config.Helpers_Destination.Directory,
                Config.Helpers_Destination.Namespace);
            ListDataSource.GenerateCode();
            var listDataSourceFullName = Path.Combine(ListDataSource.Directory, ListDataSource.FileName);
            Spc.AddSource(listDataSourceFullName, SourceText.From(ListDataSource.Code, Encoding.UTF8));

            var Forms = CrudlContext.Crudls
                .Where(a => a.Dto != null)
                .Select(crudl => new AutoFormGenerator(
                    crudl,
                    ItemDataSource,
                    ListDataSource,
                    ServiceContext.FormFile,
                    ServiceContext.ToFormFileAsyncExtention,
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
                    crudl,
                    ItemDataSource,
                    Config.Components_Destination.Directory,
                    Config.Components_Destination.Namespace))
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
                    crudl,
                    ItemDataSource,
                    ListDataSource,
                    ServiceContext.BaseListResponseT,
                    Config.Components_Destination.Directory,
                    Config.Components_Destination.Namespace))
                .ToArray();
            foreach (var list in Lists)
            {
                list.GenerateCode();
                var formFieldsViewFullName = Path.Combine(list.Directory, list.FileName);
                Spc.AddSource(formFieldsViewFullName, SourceText.From(list.Code, Encoding.UTF8));
            }

            var DropDowns = CrudlContext.Crudls
                .Where(a => a.Dto != null)
                .Select(crudl => new AutoDropDownGenerator(
                    crudl,
                    ItemDataSource,
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
                    crudl,
                    ItemDataSource,
                    ListDataSource,
                    ServiceContext.FormFile,
                    ServiceContext.ToFormFileAsyncExtention,
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
        public ClientConfig Config { get; }
        public CrudlContext CrudlContext { get; }
        public Microsoft.CodeAnalysis.SourceProductionContext Spc { get; }
        //public StateChangedHandlerGenerator StateChangedHandler { get; }
        //public IClientAuthenticationServiceGenerator IClientAuthenticationService { get; }
        //public ClientAuthenticationServiceGenerator ClientAuthenticationService { get; }
        //public ItemDataSourceGenerator ItemDataSource { get; }
        //public ListDataSourceGenerator ListDataSource { get; }
        //public AutoFormGenerator[] Forms { get; }
        //public AutoDetailsGenerator[] Details { get; }
        //public AutoListGenerator[] Lists { get; }
        //public AutoDropDownGenerator[] DropDowns { get; }
        //public AutoGridEditGenerator[] GridEdits { get; }
    }
}
