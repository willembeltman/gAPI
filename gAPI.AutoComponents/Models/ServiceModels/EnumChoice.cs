using Microsoft.CodeAnalysis;

namespace gAPI.AutoComponents.Models.ServiceModels
{
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
}