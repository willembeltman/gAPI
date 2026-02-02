using gAPI.AutoPage.Enums;

namespace gAPI.AutoPage.Interfaces;

public interface ICrudlMethod
{
    ISharedReference Client { get; }
    ISharedReference Interface { get; }
    bool IsAuthorized { get; }
    bool IsNotAuthorized { get; }
    ITypeHelper ResponseType { get; }
    ISharedReference ResponseTypeDigger { get; }
    string? IsPageRoute { get; }
    string Name { get; }
    string? IsPageTitle { get; }
    string? IsPageSubmitText { get; }
    string? IsPageResponseText { get; }
    ICrudlMethodArgument[] Arguments { get; }
    CrudlMethodTypeEnum CrudlMethodType { get; }
    string? IsComponentTitle { get; }
    string? IsComponentSubmitText { get; }
    string? IsComponentResponseText { get; }
}