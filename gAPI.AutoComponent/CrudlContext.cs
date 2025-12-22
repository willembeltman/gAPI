using gAPI.AutoComponent.Enums;
using gAPI.AutoComponent.Models.CrudlModels;
using gAPI.AutoComponent.Models.PageModels;
using System.Collections.Generic;
using System.Linq;

namespace gAPI.AutoComponent
{
    public class CrudlContext
    {
        public CrudlContext(ServiceContext serviceContext)
        {
            ServiceContext = serviceContext;

            var crudlMethods = new List<CrudlMethod>();
            var pageMethods = new List<Page>();
            foreach (var @interface in serviceContext.Interfaces)
            {
                var crudlMethodsOfInterface = @interface.Methods
                    .Where(a =>
                        a.CrudlMethodType != CrudlMethodTypeEnum.NotSet &&
                        a.CrudlMethodType != CrudlMethodTypeEnum.IsPage);
                foreach (var crudlMethodOfInterface in crudlMethodsOfInterface)
                {
                    var crudlMethodType = crudlMethodOfInterface.CrudlMethodType;
                    var responseType =
                        crudlMethodOfInterface.IsDelete ? crudlMethodOfInterface.IsDeleteType! :
                        crudlMethodOfInterface.IsFileDelete ? crudlMethodOfInterface.IsFileDeleteType! :
                        crudlMethodOfInterface.ResponseTypeDigger.Type;

                    crudlMethods.Add(new CrudlMethod(
                        this,
                        @interface,
                        crudlMethodOfInterface,
                        crudlMethodType,
                        responseType));
                }

                var pageMethodsOfInterface = @interface.Methods
                    .Where(a =>
                        a.CrudlMethodType == CrudlMethodTypeEnum.IsPage);
                foreach (var pageMethodOfInterface in pageMethodsOfInterface)
                {
                    pageMethods.Add(new Page(
                        this,
                        @interface,
                        pageMethodOfInterface,
                        pageMethodOfInterface.ResponseTypeDigger.Type));
                }
            }
            AllCrudlMethods = crudlMethods
                .ToArray();
            Crudls = crudlMethods
                .GroupBy(a => a.Type)
                .Select(a => new CrudlType(
                    this,
                    a.Key,
                    a.ToArray()))
                .Where(a => a.Dto != null)
                .ToArray();
            Pages = pageMethods
                .ToArray();
        }

        public ServiceContext ServiceContext { get; }
        public CrudlMethod[] AllCrudlMethods { get; }
        public CrudlType[] Crudls { get; }
        public Page[] Pages { get; }
    }
}