//using gAPI.CodeGen.Backend.Generators.Business.Interfaces;
//using gAPI.CodeGen.Backend.Generators.Business.Models;
//using gAPI.CodeGen.Backend.Generators.Shared.Dtos;
//using gAPI.CodeGen.Backend.Generators.Shared.ResponseDtos;
//using gAPI.CodeGen.Backend.Helpers;
//using gAPI.CodeGen.Backend.Models.Entities;

//namespace gAPI.CodeGen.Backend.Generators.Business.CrudInterfaces
//{
//    public class CrudHandlerInterfaceGenerator : BaseGenerator
//    {
//        public CrudHandlerInterfaceGenerator(
//            DtoGenerator dto,
//            DirectoryInfo directory,
//            string @namespace)
//        {
//            Dto = dto;
//            Directory = directory;
//            Namespace = @namespace;

//            DbSet = Dto.DbSet;
//            Entity = DbSet.Entity;

//            Context = Dto.Context;
//            IServerAuthenticationService = Context.IServerAuthenticationService;
//            AuthenticationState = IServerAuthenticationService.AuthenticationState;
//            BaseResponseT = Context.BaseResponseT;

//            StateDto = Context.StateDto;
//            DbContext = StateDto.DbContext;

//            Name = $"I{Entity.Name!.ToMultiple()}Handler";
//            FileName = $"{Name}.cs";
//        }

//        public DtoGenerator Dto { get; }
//        public DbSet DbSet { get; }
//        public Entity Entity { get; }
//        public IServerAuthenticationServiceGenerator IServerAuthenticationService { get; }
//        public BackendGenerator Context { get; }
//        public AuthenticationStateGenerator AuthenticationState { get; }
//        public BaseResponseTGenerator BaseResponseT { get; }
//        public StateDtoGenerator StateDto { get; }
//        public DbContext DbContext { get; }

//        public void GenerateCode()
//        {
//            var keyType = Entity.Properties.FirstOrDefault(a => a.IsKey)?.TypeSimpleName ?? "long";

//            Reg(AuthenticationState);
//            Reg(DbContext.Type);
//            Reg(Entity.Type);
//            Reg("Microsoft.EntityFrameworkCore");
//            Reg(IServerAuthenticationService);
//            if (Entity.IsStorageFile)
//                Reg("gAPI.Storage");

//            Code = $@"{GetNamespacesCode()}namespace {Namespace};

//public interface {Name}
//{{
//    Task InitializeAsync();
//    AuthenticationState? AuthenticationState {{ get; }} 

//    Task<bool> IsAllowedAsync();
//    Task<bool> CanCreateAsync();
//    Task<bool> CanListAsync();
//    Task<bool> CanCreateAsync({Dto.FullName} dto);
//    Task<bool> CanReadAsync({Dto.FullName} dto);
//    Task<bool> CanUpdateAsync({Dto.FullName} dto);
//    Task<bool> CanDeleteAsync({Dto.FullName} dto);

//    Task<{Entity.Name}?> FindByMatchAsync({Dto.FullName} dto);
//    Task<{Entity.Name}?> FindByIdAsync({keyType} id);
//    IQueryable<{Entity.Name}> ListAll();

//    Task AddAsync({Entity.Name} entity);
//    Task RemoveAsync({Entity.Name} entity);

//    Task<bool> AddAsync({Entity.Name} entity);
//    Task<bool> UpdateAsync({Entity.Name} entity, {Dto.FullName} dto);
//    Task<bool> RemoveAsync({Entity.Name} entity);

//    Task SaveChangesAsync();
//}}
//";
//            Save(false);
//        }
//    }
//}