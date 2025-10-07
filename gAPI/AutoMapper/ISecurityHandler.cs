using System.Threading.Tasks;

namespace gAPI.AutoMapper
{
    public interface ISecurityHandler<TDto>
    {
        Task<bool> CanCreateAsync();
        Task<bool> CanCreateAsync(TDto dto);
        Task<bool> CanReadAsync(TDto dto);
        Task<bool> CanRemoveAsync(TDto dto);
        Task<bool> CanUpdateAsync(TDto dto);
        Task<bool> CanListAsync();
    }
}