using Application.Interfaces.Repository;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class UserBehaviorRepository(AuthDbContext db) : IUserBehaviorRepository
    {
        private readonly AuthDbContext _db = db;

		public async Task<UserBehaviorProfile> GetByUserIdAsync(Guid userId)
        {
            return await _db.UserBehaviorProfiles.FirstOrDefaultAsync(x => x.UserId == userId);
        }

        public async Task AddAsync(UserBehaviorProfile profile)
        {
            await _db.UserBehaviorProfiles.AddAsync(profile);
            await _db.SaveChangesAsync();
        }

        public async Task UpdateAsync(UserBehaviorProfile profile)
        {
            _db.UserBehaviorProfiles.Update(profile);
            await _db.SaveChangesAsync();
        }
    }
}
