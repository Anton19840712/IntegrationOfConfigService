using Application.Interfaces.Repository;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class UserRoleRepository : IUserRoleRepository
    {
        private readonly AuthDbContext _db;
        public UserRoleRepository(AuthDbContext db) => _db = db;

        public async Task<UserRole> GetAsync(Guid userId, Guid roleId) =>
            await _db.UserRoles.FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

        public async Task<IReadOnlyList<UserRole>> GetByUserIdAsync(Guid userId) =>
            await _db.UserRoles
                .Include(ur => ur.Role)
                .Where(ur => ur.UserId == userId)
                .ToListAsync();

        public async Task<IReadOnlyList<UserRole>> GetByRoleIdAsync(Guid roleId) =>
            await _db.UserRoles
                .Include(ur => ur.User)
                .Where(ur => ur.RoleId == roleId)
                .ToListAsync();

        public async Task AddAsync(UserRole userRole)
        {
            await _db.UserRoles.AddAsync(userRole);
            await _db.SaveChangesAsync();
        }

        public void Delete(UserRole userRole)
        {
            _db.UserRoles.Remove(userRole);
            _db.SaveChanges();
        }

        public async Task<List<UserRole>> GetRolesForUserAsync(Guid userId)
        {
            return await _db.UserRoles
                .Where(ur => ur.UserId == userId)
                .ToListAsync();
        }

        public async Task AddRangeAsync(IEnumerable<UserRole> userRoles)
        {
            await _db.UserRoles.AddRangeAsync(userRoles);
            await _db.SaveChangesAsync(); 
        }

        public void DeleteRange(IEnumerable<UserRole> userRoles)
        {
            _db.UserRoles.RemoveRange(userRoles);
            _db.SaveChanges(); 
        }
    }
}
