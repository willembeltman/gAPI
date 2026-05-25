using gAPI.CodeGen.Frontend.Enums;
using gAPI.CodeGen.Frontend.Models.CrudsModels;
using gAPI.CodeGen.Frontend.Models.ServiceModels;

namespace gAPI.CodeGen.Frontend.Models;

public class CrudContext
{
    public CrudContext(ServiceContext serviceContext, SharedReferences sharedReferences)
    {
        ServiceContext = serviceContext;
        SharedReferences = sharedReferences;

        var modelMethods = new List<CrudMethod>();
        var pageMethods = new List<InterfaceMethod>();
        foreach (var @interface in serviceContext.Interfaces)
        {
            var methods1 = @interface.Methods
                .Where(a =>
                    a.CrudMethodType != CrudMethodTypeEnum.Page &&
                    a.CrudMethodType != CrudMethodTypeEnum.NotSet);
            foreach (var method in methods1)
            {
                var crudMethodType = method.CrudMethodType;
                var responseType =
                    method.IsDelete ? method.IsDeleteType :
                    method.IsFileDelete ? method.IsFileDeleteType :
                    method.ResponseTypeDigger.Type;

                modelMethods.Add(new CrudMethod(
                    this,
                    @interface,
                    method,
                    crudMethodType,
                    responseType!));
            }
            var methods2 = @interface.Methods
                .Where(a =>
                    a.CrudMethodType == CrudMethodTypeEnum.Page);
            foreach (var method in methods2)
            {
                pageMethods.Add(method);
            }
        }
        AllCrudMethods = [.. modelMethods];
        AllCrudTypes = [.. modelMethods
            .GroupBy(a => a.ResponseRealType)
            .Select(a => new CrudType(
                this,
                a.Key,
                a.ToArray()))];
        AllPageMethods = [.. pageMethods];
    }
    public ServiceContext ServiceContext { get; }
    public SharedReferences SharedReferences { get; }

    public CrudMethod[] AllCrudMethods { get; }
    public InterfaceMethod[] AllPageMethods { get; }
    public CrudType[] AllCrudTypes { get; }
}