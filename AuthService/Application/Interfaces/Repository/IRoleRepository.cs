using Domain.Entities;

namespace Application.Interfaces.Repository
{
    public interface IRoleRepository
    {
        Task<Role> GetByIdAsync(Guid id);
        Task<Role> GetByNameAsync(string name);
        Task<IReadOnlyList<Role>> GetAllAsync();
        Task AddAsync(Role role);
        void Update(Role role);
        void Delete(Role role);
        Task<IReadOnlyList<Role>> GetAllWithPrivilegesAsync();
        Task<Role> GetByIdWithPrivilegesAsync(Guid id);
    }
}
