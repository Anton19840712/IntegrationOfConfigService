using Application.Interfaces.Repository;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class RoleRepository(AuthDbContext db) : IRoleRepository
    {
        private readonly AuthDbContext _db = db;

		public async Task<Role> GetByIdAsync(Guid id) =>
            await _db.Roles
                .Include(r => r.RolePrivileges)
                    .ThenInclude(rp => rp.Privilege)
                .FirstOrDefaultAsync(r => r.Id == id);

        public async Task<Role> GetByNameAsync(string name) =>
            await _db.Roles.FirstOrDefaultAsync(r => r.Name == name);

        public async Task<IReadOnlyList<Role>> GetAllAsync() =>
            await _db.Roles.ToListAsync();

        public async Task AddAsync(Role role)
        {
            await _db.Roles.AddAsync(role);
            await _db.SaveChangesAsync();
        }

        public void Update(Role role)
        {
            _db.Roles.Update(role);
            _db.SaveChanges();
        }

        public void Delete(Role role)
        {
            _db.Roles.Remove(role);
            _db.SaveChanges();
        }

        public async Task<IReadOnlyList<Role>> GetAllWithPrivilegesAsync()
        {
            return await _db.Roles
                .Include(r => r.RolePrivileges)
                    .ThenInclude(rp => rp.Privilege)
                .ToListAsync();
        }

        public async Task<Role> GetByIdWithPrivilegesAsync(Guid id)
        {
            return await _db.Roles
                    .Include(r => r.RolePrivileges)
                    .ThenInclude(rp => rp.Privilege)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(r => r.Id == id);
        }
    }
}
