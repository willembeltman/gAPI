namespace gAPI.AutoComponent.Interfaces
{
    public interface ISharedReferences
    {
        ISharedReference[] AllComponents { get; }
        ISharedReference ListDataSource { get; }
        ISharedReference? IClientConnection { get; }
        ISharedReference State { get; }
    }
}