namespace gAPI.AutoPage.Interfaces
{
    public interface ISharedReferences
    {
        ISharedReference[] AllComponents { get; }
        ISharedReference ListDataSource { get; }
    }
}