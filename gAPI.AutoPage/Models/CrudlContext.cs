using gAPI.AutoPage.Enums;
using gAPI.AutoPage.Models.CrudlModels;
using System.Collections.Generic;
using System.Linq;

namespace gAPI.AutoPage.Models;

public class CrudlContext
{
    public CrudlContext(ServiceContext serviceContext)
    {
        ServiceContext = serviceContext;

        var crudlMethods = new List<CrudlMethod>();
        var pageMethods = new List<CrudlMethod>();
        var componentMethods = new List<CrudlMethod>();
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
            foreach (var crudlMethodOfInterface in pageMethodsOfInterface)
            {
                var crudlMethodType = crudlMethodOfInterface.CrudlMethodType;
                var responseType = crudlMethodOfInterface.ResponseType;

                pageMethods.Add(new CrudlMethod(
                    this,
                    @interface,
                    crudlMethodOfInterface,
                    crudlMethodType,
                    responseType));
            }

            var componentMethodsOfInterface = @interface.Methods
                .Where(a =>
                    a.CrudlMethodType == CrudlMethodTypeEnum.IsComponent);
            foreach (var crudlMethodOfInterface in componentMethodsOfInterface)
            {
                var crudlMethodType = crudlMethodOfInterface.CrudlMethodType;
                var responseType = crudlMethodOfInterface.ResponseType;

                componentMethods.Add(new CrudlMethod(
                    this,
                    @interface,
                    crudlMethodOfInterface,
                    crudlMethodType,
                    responseType));
            }
        }
        AllCrudlMethods = [.. crudlMethods];
        Crudls = [.. crudlMethods
            .GroupBy(a => a.ResponseType)
            .Select(a => new CrudlType(
                this,
                a.Key,
                [.. a]))
            .Where(a => a.Dto != null)];
        PageMethods = [.. pageMethods];
        ComponentMethods = [.. componentMethods];
    }

    public ServiceContext ServiceContext { get; }
    public CrudlMethod[] AllCrudlMethods { get; }
    public CrudlType[] Crudls { get; }
    public CrudlMethod[] PageMethods { get; }
    public CrudlMethod[] ComponentMethods { get; }
}