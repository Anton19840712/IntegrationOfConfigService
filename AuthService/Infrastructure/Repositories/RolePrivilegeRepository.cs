using Application.Interfaces.Repository;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class RolePrivilegeRepository : IRolePrivilegeRepository
    {
        private readonly AuthDbContext _db;
        public RolePrivilegeRepository(AuthDbContext db) => _db = db;

        public async Task<RolePrivilege> GetAsync(Guid roleId, Guid privilegeId) =>
            await _db.RolePrivileges.FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PrivilegeId == privilegeId);

        public async Task<IReadOnlyList<RolePrivilege>> GetByRoleIdAsync(Guid roleId) =>
            await _db.RolePrivileges
                .Include(rp => rp.Privilege)
                .Where(rp => rp.RoleId == roleId)
                .ToListAsync();

        public async Task<IReadOnlyList<RolePrivilege>> GetByPrivilegeIdAsync(Guid privilegeId) =>
            await _db.RolePrivileges
                .Include(rp => rp.Role)
                .Where(rp => rp.PrivilegeId == privilegeId)
                .ToListAsync();

        public async Task AddAsync(RolePrivilege rolePrivilege)
        {
            await _db.RolePrivileges.AddAsync(rolePrivilege);
            await _db.SaveChangesAsync();
        }

        public async Task<List<RolePrivilege>> GetPrivilegesForRoleAsync(Guid roleId)
        {
            return await _db.RolePrivileges.Where(rp => rp.RoleId == roleId).ToListAsync();
        }

        public async Task AddRangeAsync(IEnumerable<RolePrivilege> rolePrivileges)
        {
            await _db.RolePrivileges.AddRangeAsync(rolePrivileges);
        }

        public void DeleteRange(IEnumerable<RolePrivilege> rolePrivileges)
        {
            _db.RolePrivileges.RemoveRange(rolePrivileges);
        }

        public void Delete(RolePrivilege rolePrivilege)
        {
            _db.RolePrivileges.Remove(rolePrivilege);
            _db.SaveChanges();
        }
    }
}
