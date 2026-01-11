//using gAPI.AutoMapper;
//using System.Linq;
//using System.Threading.Tasks;

//namespace gAPI.Interfaces;

//public interface IHandler<TEntity, TDto, TKey> : ISecurityHandler<TDto>
//{
//    Task<TEntity?> FindByMatchAsync(TDto dto); 
//    Task<TEntity?> FindByIdAsync(TKey id);
//    IQueryable<TEntity> ListAll();
//    Task<bool> AddAsync(TEntity entity);
//    Task<bool> UpdateAsync(TEntity entity, TDto dto);
//    Task<bool> RemoveAsync(TEntity entity);
//}