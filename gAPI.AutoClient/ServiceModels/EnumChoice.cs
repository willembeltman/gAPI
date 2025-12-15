using Microsoft.CodeAnalysis;

namespace gAPI.AutoClient.ServiceModels
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