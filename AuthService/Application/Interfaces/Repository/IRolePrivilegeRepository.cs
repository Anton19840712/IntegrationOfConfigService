using Domain.Entities;

namespace Application.Interfaces.Repository
{
    public interface IRolePrivilegeRepository
    {
        Task<RolePrivilege> GetAsync(Guid roleId, Guid privilegeId);
        Task<IReadOnlyList<RolePrivilege>> GetByRoleIdAsync(Guid roleId);
        Task<IReadOnlyList<RolePrivilege>> GetByPrivilegeIdAsync(Guid privilegeId);
        Task AddAsync(RolePrivilege rolePrivilege);
        void Delete(RolePrivilege rolePrivilege);
        Task<List<RolePrivilege>> GetPrivilegesForRoleAsync(Guid roleId);
        Task AddRangeAsync(IEnumerable<RolePrivilege> rolePrivileges);
        void DeleteRange(IEnumerable<RolePrivilege> rolePrivileges);
    }
}
