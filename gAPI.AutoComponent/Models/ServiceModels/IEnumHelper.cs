using gAPI.AutoComponent.Interfaces;

namespace gAPI.AutoComponent.Models.ServiceModels
{
    public interface IEnumHelper : ISharedReference
    {
        IEnumChoice[] Choices { get; }
    }
}