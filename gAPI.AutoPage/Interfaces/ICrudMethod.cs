using gAPI.AutoPage.Enums;

namespace gAPI.AutoPage.Interfaces;

public interface ICrudMethod
{
    ISharedReference Client { get; }
    ISharedReference Interface { get; }
    ITypeHelper Type { get; }
    ITypeDigger TypeDigger { get; }
    ICrudMethodArgument[] Arguments { get; }
    CrudMethodTypeEnum CrudMethodType { get; }
    bool IsAuthorized { get; }
    bool IsNotAuthorized { get; }
    string? IsPageRoute { get; }
    string Name { get; }
    string? IsPageTitle { get; }
    string? IsPageSubmitText { get; }
    string? IsPageResponseText { get; }
    string? IsComponentTitle { get; }
    string? IsComponentSubmitText { get; }
    string? IsComponentResponseText { get; }
    string? ForeignKeyName { get; }
}