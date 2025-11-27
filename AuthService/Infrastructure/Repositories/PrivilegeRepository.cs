using Application.Interfaces.Repository;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class PrivilegeRepository : IPrivilegeRepository
    {
        private readonly AuthDbContext _db;
        public PrivilegeRepository(AuthDbContext db) => _db = db;

        public async Task<Privilege> GetByIdAsync(Guid id) =>
            await _db.Privileges
                .Include(p => p.RolePrivileges)
                    .ThenInclude(rp => rp.Role)
                .FirstOrDefaultAsync(p => p.Id == id);

        public async Task<Privilege> GetByNameAsync(string name) =>
            await _db.Privileges.FirstOrDefaultAsync(p => p.Name == name);

        public async Task<IReadOnlyList<Privilege>> GetAllAsync() =>
            await _db.Privileges.ToListAsync();

        public async Task AddAsync(Privilege privilege)
        {
            await _db.Privileges.AddAsync(privilege);
            await _db.SaveChangesAsync();
        }

        public void Update(Privilege privilege)
        {
            _db.Privileges.Update(privilege);
            _db.SaveChanges();
        }

        public void Delete(Privilege privilege)
        {
            _db.Privileges.Remove(privilege);
            _db.SaveChanges();
        }
    }
}
