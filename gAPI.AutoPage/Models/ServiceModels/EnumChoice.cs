using Microsoft.CodeAnalysis;

namespace gAPI.AutoPage.Models.ServiceModels;

public class EnumChoice : IEnumChoice
{
    public EnumChoice(IEnumHelper @enum, IFieldSymbol fieldSymbol)
    {
        Enum = @enum;
        FieldSymbol = fieldSymbol;
    }

    public IEnumHelper Enum { get; }
    private IFieldSymbol FieldSymbol { get; }
    public string Name => FieldSymbol.Name;
}