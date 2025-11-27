using Application.Interfaces.Repository;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class UserRepository(AuthDbContext context) : IUserRepository
    {
        private readonly AuthDbContext _context = context;

		public async Task<User> GetByIdAsync(Guid id)
        {
            return await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                        .ThenInclude(r => r.RolePrivileges)
                            .ThenInclude(rp => rp.Privilege)
                .Include(u => u.RefreshTokens)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<User> GetByIdWithPreviegesAsync(Guid id)
        {
            return await _context.Users
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                            .ThenInclude(r => r.RolePrivileges)
                                .ThenInclude(rp => rp.Privilege)
                    .AsNoTracking() 
                    .FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<User> GetByLoginAsync(string login)
        {
            return await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                        .ThenInclude(r => r.RolePrivileges)
                            .ThenInclude(rp => rp.Privilege)
                .Include(u => u.RefreshTokens)
                .FirstOrDefaultAsync(u => u.Login == login);
        }

        public async Task<User> GetByEmailAsync(string email)
        {
            return await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                        .ThenInclude(r => r.RolePrivileges)
                            .ThenInclude(rp => rp.Privilege)
                .Include(u => u.RefreshTokens)
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<IReadOnlyList<User>> GetAllAsync()
        {
            return await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.RolePrivileges)
                    .ThenInclude(rp => rp.Privilege)
                .Include(u => u.RefreshTokens)
                .ToListAsync();
        }

        public async Task AddAsync(User user)
        {
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
        }

        public void Update(User user)
        {
            _context.Users.Update(user);
            _context.SaveChanges();
        }

        public void Delete(User user)
        {
            _context.Users.Remove(user);
            _context.SaveChanges();
        }

        public async Task<bool> AnyWithLoginOrEmailAsync(string login, string email)
        {
            return await _context.Users.AnyAsync(u => u.Login == login || u.Email == email);
        }

    }
}
