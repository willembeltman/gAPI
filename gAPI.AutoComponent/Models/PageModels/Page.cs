using gAPI.AutoComponent.Models.ServiceModels;

namespace gAPI.AutoComponent.Models.PageModels;

public class Page
{
    public Page(CrudContext viewsContext, Interface @interface, InterfaceMethod method, TypeHelper responseType)
    {
        ViewsContext = viewsContext;
        Interface = @interface;
        InterfaceMethod = method;
        ResponseType = responseType;
    }

    public CrudContext ViewsContext { get; }
    public Interface Interface { get; }
    public InterfaceMethod InterfaceMethod { get; }
    public TypeHelper ResponseType { get; }
}