using gAPI.AutoPage.Interfaces;

namespace gAPI.AutoPage.Models.ServiceModels
{
    public interface ITypeHelperPropertyAttribute : ISharedReference
    {
        ITypeHelper Type { get; }

        string ToNameString();
        string ToString();
    }
}