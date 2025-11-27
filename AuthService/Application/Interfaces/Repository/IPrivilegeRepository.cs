using Domain.Entities;

namespace Application.Interfaces.Repository
{
    public interface IPrivilegeRepository
    {
        Task<Privilege> GetByIdAsync(Guid id);
        Task<Privilege> GetByNameAsync(string name);
        Task<IReadOnlyList<Privilege>> GetAllAsync();
        Task AddAsync(Privilege privilege);
        void Update(Privilege privilege);
        void Delete(Privilege privilege);
    }
}
