namespace gAPI.Interfaces
{
    public interface ICrudEntity
    {
        bool CanUpdate { get; set; }
        bool CanRemove { get; set; }
    }
}