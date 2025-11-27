using Domain.Entities;

namespace Application.Interfaces.Repository
{
    public interface IServiceClientRepository
    {
        Task<ServiceClient> GetByIdAsync(Guid id);
        Task<ServiceClient> GetByClientIdAsync(string clientId);
        Task<IReadOnlyList<ServiceClient>> GetAllAsync();
        Task<IReadOnlyList<ServiceClient>> FindByNameAsync(string namePart);
        Task AddAsync(ServiceClient client);
        void Update(ServiceClient client);
        Task DeleteAsync(Guid id);
        Task DeleteAsync(ServiceClient client);
        Task<bool> ExistsAsync(string clientId);
    }
}
