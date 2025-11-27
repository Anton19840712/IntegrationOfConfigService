using Domain.Entities;

namespace Application.Interfaces.Repository
{
    public interface IUserRepository
    {
        Task<User> GetByIdAsync(Guid id);
        Task<User> GetByIdWithPreviegesAsync(Guid id);
        Task<User> GetByLoginAsync(string login);
        Task<User> GetByEmailAsync(string email);
        Task<IReadOnlyList<User>> GetAllAsync();
        Task AddAsync(User user);
        void Update(User user);
        void Delete(User user);
        Task<bool> AnyWithLoginOrEmailAsync(string login, string email);
    
    }
}
