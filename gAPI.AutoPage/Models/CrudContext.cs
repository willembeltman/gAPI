using gAPI.AutoPage.Enums;
using gAPI.AutoPage.Models.CrudModels;
using System.Collections.Generic;
using System.Linq;

namespace gAPI.AutoPage.Models;

public class CrudContext
{
    public CrudContext(ServiceContext serviceContext)
    {
        ServiceContext = serviceContext;

        var crudMethods = new List<CrudMethod>();
        var pageMethods = new List<CrudMethod>();
        var componentMethods = new List<CrudMethod>();
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
            foreach (var crudMethodOfInterface in pageMethodsOfInterface)
            {
                var crudMethodType = crudMethodOfInterface.CrudMethodType;
                var responseType = crudMethodOfInterface.Type;

                pageMethods.Add(new CrudMethod(
                    this,
                    @interface,
                    crudMethodOfInterface,
                    crudMethodType,
                    responseType));
            }

            var componentMethodsOfInterface = @interface.Methods
                .Where(a =>
                    a.CrudMethodType == CrudMethodTypeEnum.IsComponent);
            foreach (var crudMethodOfInterface in componentMethodsOfInterface)
            {
                var crudMethodType = crudMethodOfInterface.CrudMethodType;
                var responseType = crudMethodOfInterface.Type;

                componentMethods.Add(new CrudMethod(
                    this,
                    @interface,
                    crudMethodOfInterface,
                    crudMethodType,
                    responseType));
            }
        }
        AllCrudMethods = [.. crudMethods];
        Cruds = [.. crudMethods
            .GroupBy(a => a.Type)
            .Select(a => new CrudType(
                this,
                a.Key,
                [.. a]))
            .Where(a => a.ResponseTypeBase != null)];
        PageMethods = [.. pageMethods];
        ComponentMethods = [.. componentMethods];
    }

    public ServiceContext ServiceContext { get; }
    public CrudMethod[] AllCrudMethods { get; }
    public CrudType[] Cruds { get; }
    public CrudMethod[] PageMethods { get; }
    public CrudMethod[] ComponentMethods { get; }
}