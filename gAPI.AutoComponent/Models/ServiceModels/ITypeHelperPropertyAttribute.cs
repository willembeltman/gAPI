using gAPI.AutoComponent.Interfaces;

namespace gAPI.AutoComponent.Models.ServiceModels
{
    public interface ITypeHelperPropertyAttribute : ISharedReference
    {
        ITypeHelper Type { get; }

        string ToNameString();
        string ToString();
    }
}