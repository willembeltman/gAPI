using gAPI.AutoComponents.Contexts;
using gAPI.AutoComponents.Helpers;
using gAPI.AutoComponents.Models.ServiceModels;

namespace gAPI.AutoComponents.Models.PageModels
{
    public class Page
    {
        public Page(CrudlContext viewsContext, Interface @interface, InterfaceMethod method, TypeHelper responseType)
        {
            ViewsContext = viewsContext;
            Interface = @interface;
            InterfaceMethod = method;
            ResponseType = responseType;
        }

        public CrudlContext ViewsContext { get; }
        public Interface Interface { get; }
        public InterfaceMethod InterfaceMethod { get; }
        public TypeHelper ResponseType { get; }
        public InterfaceMethodArgument[] Arguments => InterfaceMethod.Arguments;
    }
}