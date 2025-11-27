using Domain.Entities;

namespace Application.Interfaces.Repository
{
    public interface IUserRoleRepository
    {
        Task<UserRole> GetAsync(Guid userId, Guid roleId);
        Task<IReadOnlyList<UserRole>> GetByUserIdAsync(Guid userId);
        Task<IReadOnlyList<UserRole>> GetByRoleIdAsync(Guid roleId);
        Task<List<UserRole>> GetRolesForUserAsync(Guid userId);
        Task AddRangeAsync(IEnumerable<UserRole> userRoles);
        void DeleteRange(IEnumerable<UserRole> userRoles);
        Task AddAsync(UserRole userRole);
        void Delete(UserRole userRole);
    }
}
