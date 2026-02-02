using Microsoft.CodeAnalysis;

namespace gAPI.AutoPage.Models.ServiceModels;

public class EnumChoice
{
    public EnumChoice(EnumDto @enum, IFieldSymbol fieldSymbol)
    {
        Enum = @enum;
        FieldSymbol = fieldSymbol;
    }

    public EnumDto Enum { get; }
    public IFieldSymbol FieldSymbol { get; }
}