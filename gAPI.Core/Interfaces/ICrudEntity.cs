namespace gAPI.Core.Interfaces;

public interface ICrudEntity
{
    bool CanUpdate { get; set; }
    bool CanDelete { get; set; }
}