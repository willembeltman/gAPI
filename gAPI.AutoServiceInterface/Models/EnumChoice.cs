using Microsoft.CodeAnalysis;

namespace gAPI.AutoServiceInterface.Models
{
    internal class EnumChoice
    {
        public EnumChoice(EnumDto @enum, IFieldSymbol fieldSymbol)
        {
            Enum = @enum;
            FieldSymbol = fieldSymbol;
        }

        public EnumDto Enum { get; }
        public IFieldSymbol FieldSymbol { get; }
    }
}