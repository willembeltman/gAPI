namespace gAPI.AutoComponent.Interfaces
{
    public interface IPage : ISharedReference
    {
        string Route { get; }
        string Title { get; }
        bool IsAuthorized { get; }
        bool IsNotAuthorized { get; }
    }
}