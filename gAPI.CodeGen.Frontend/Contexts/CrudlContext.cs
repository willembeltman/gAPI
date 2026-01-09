using gAPI.CodeGen.Frontend.Enums;
using gAPI.CodeGen.Frontend.Models.CrudlsModels;
using gAPI.CodeGen.Frontend.Models.ServiceModels;

namespace gAPI.CodeGen.Frontend.Contexts;

public class CrudlContext
{
    public CrudlContext(ServiceContext dataModel)
    {
        ServiceContext = dataModel;

        var modelMethods = new List<CrudlMethod>();
        var pageMethods = new List<InterfaceMethod>();
        foreach (var @interface in dataModel.Interfaces)
        {
            var methods1 = @interface.Methods
                .Where(a =>
                    a.CrudlMethodType != CrudlMethodTypeEnum.Page &&
                    a.CrudlMethodType != CrudlMethodTypeEnum.NotSet);
            foreach (var method in methods1)
            {
                var crudlMethodType = method.CrudlMethodType;
                var responseType =
                    method.IsDelete ? method.IsDeleteType :
                    method.IsFileDelete ? method.IsFileDeleteType :
                    method.ResponseTypeDigger.Type;

                modelMethods.Add(new CrudlMethod(
                    this,
                    @interface,
                    method,
                    crudlMethodType,
                    responseType!));
            }
            var methods2 = @interface.Methods
                .Where(a =>
                    a.CrudlMethodType == CrudlMethodTypeEnum.Page);
            foreach (var method in methods2)
            {
                pageMethods.Add(method);
            }
        }
        AllMethods = modelMethods
            .ToArray();
        AllPageMethods = pageMethods
            .ToArray();
        Types = modelMethods
            .GroupBy(a => a.ResponseRealType)
            .Select(a => new CrudlType(
                this,
                a.Key,
                a.ToArray()))
            .ToArray();
    }
    public ServiceContext ServiceContext { get; }
    public CrudlMethod[] AllMethods { get; }
    public InterfaceMethod[] AllPageMethods { get; }
    public CrudlType[] Types { get; }
}