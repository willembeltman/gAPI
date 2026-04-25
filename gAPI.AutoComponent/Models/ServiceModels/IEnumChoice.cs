namespace gAPI.AutoComponent.Models.ServiceModels
{
    public interface IEnumChoice
    {
        IEnumHelper Enum { get; }
        string Name { get; }
    }
}