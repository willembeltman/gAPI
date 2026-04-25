using gAPI.AutoPage.Interfaces;

namespace gAPI.AutoPage.Models.ServiceModels
{
    public interface IEnumHelper : ISharedReference
    {
        IEnumChoice[] Choices { get; }
    }
}