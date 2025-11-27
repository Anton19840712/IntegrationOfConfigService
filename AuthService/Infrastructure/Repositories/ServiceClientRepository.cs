using Application.Interfaces.Repository;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class ServiceClientRepository : IServiceClientRepository
    {
        private readonly AuthDbContext _db;
        public ServiceClientRepository(AuthDbContext db) => _db = db;

        public async Task<ServiceClient> GetByIdAsync(Guid id)
        {
            return await _db.Set<ServiceClient>()
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<ServiceClient> GetByClientIdAsync(string clientId)
        {
            return await _db.Set<ServiceClient>()
                .FirstOrDefaultAsync(c => c.ClientId == clientId);
        }

        public async Task<IReadOnlyList<ServiceClient>> GetAllAsync()
        {
            return await _db.Set<ServiceClient>()
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<ServiceClient>> FindByNameAsync(string namePart)
        {
            return await _db.Set<ServiceClient>()
                .Where(c => c.Name.Contains(namePart))
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task AddAsync(ServiceClient client)
        {
            await _db.Set<ServiceClient>().AddAsync(client);
            await _db.SaveChangesAsync();
        }

        public void Update(ServiceClient client)
        {
            _db.Set<ServiceClient>().Update(client);
            _db.SaveChanges();
        }

        public async Task DeleteAsync(Guid id)
        {
            var client = await GetByIdAsync(id);
            if (client == null) return;
            _db.Set<ServiceClient>().Remove(client);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(ServiceClient client)
        {
            if (client == null) return;
            _db.Set<ServiceClient>().Remove(client);
            await _db.SaveChangesAsync();
        }

        public async Task<bool> ExistsAsync(string clientId)
        {
            return await _db.Set<ServiceClient>().AnyAsync(c => c.ClientId == clientId);
        }
    }
}
