using Domain.Entities;

namespace Application.Interfaces.Repository
{
    public interface IUserBehaviorRepository
    {
        Task<UserBehaviorProfile> GetByUserIdAsync(Guid userId);
        Task AddAsync(UserBehaviorProfile profile);
        Task UpdateAsync(UserBehaviorProfile profile);
    }
}
