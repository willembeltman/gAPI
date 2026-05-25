using gAPI.AutoComponent.Enums;
using gAPI.AutoComponent.Models.CrudModels;
using gAPI.AutoComponent.Models.PageModels;
using System.Collections.Generic;
using System.Linq;

namespace gAPI.AutoComponent.Models;

public class CrudContext
{
    public CrudContext(ServiceContext serviceContext)
    {
        ServiceContext = serviceContext;

        var crudMethods = new List<CrudMethod>();
        var pageMethods = new List<Page>();
        foreach (var @interface in serviceContext.Interfaces)
        {
            var crudMethodsOfInterface = @interface.Methods
                .Where(a =>
                    a.CrudMethodType != CrudMethodTypeEnum.NotSet &&
                    a.CrudMethodType != CrudMethodTypeEnum.IsPage);
            foreach (var crudMethodOfInterface in crudMethodsOfInterface)
            {
                var crudMethodType = crudMethodOfInterface.CrudMethodType;
                var responseType =
                    crudMethodOfInterface.IsDelete ? crudMethodOfInterface.IsDeleteType! :
                    crudMethodOfInterface.IsFileDelete ? crudMethodOfInterface.IsFileDeleteType! :
                    crudMethodOfInterface.TypeDigger.Type;

                crudMethods.Add(new CrudMethod(
                    this,
                    @interface,
                    crudMethodOfInterface,
                    crudMethodType,
                    responseType));
            }

            var pageMethodsOfInterface = @interface.Methods
                .Where(a =>
                    a.CrudMethodType == CrudMethodTypeEnum.IsPage);
            foreach (var pageMethodOfInterface in pageMethodsOfInterface)
            {
                pageMethods.Add(new Page(
                    this,
                    @interface,
                    pageMethodOfInterface,
                    pageMethodOfInterface.TypeDigger.Type));
            }
        }
        AllCrudMethods = crudMethods
            .ToArray();
        Cruds = crudMethods
            .GroupBy(a => a.Type)
            .Select(a => new CrudType(
                this,
                a.Key,
                a.ToArray()))
            .Where(a => a.ResponseType != null)
            .ToArray();
        Pages = pageMethods
            .ToArray();
    }

    public ServiceContext ServiceContext { get; }
    public CrudMethod[] AllCrudMethods { get; }
    public CrudType[] Cruds { get; }
    public Page[] Pages { get; }
}