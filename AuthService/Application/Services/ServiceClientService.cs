using Application.Interfaces.Repository;
using Application.Interfaces.Service;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using System.Security.Cryptography;

namespace Application.Services
{
    public class ServiceClientService
    {
        private readonly IServiceClientRepository _repo;
        private readonly IJwtTokenGenerator _jwt;
        private readonly IPasswordHasher<ServiceClient> _hasher;

        public ServiceClientService(IServiceClientRepository repo, IJwtTokenGenerator jwt)
        {
            _repo = repo;
            _jwt = jwt;
            _hasher = new PasswordHasher<ServiceClient>();
        }

        /// <summary>
        /// Создание нового сервисного клиента
        /// </summary>
        public async Task<(string ClientId, string ClientSecret)> CreateAsync(string name)
        {
            var clientId = Guid.NewGuid().ToString("N");
            var clientSecret = GenerateSecret();

            var client = new ServiceClient
            {
                Id = Guid.NewGuid(),
                Name = name,
                ClientId = clientId,
                ClientSecretHash = _hasher.HashPassword(null, clientSecret),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _repo.AddAsync(client);

            // секрет возвращаем только при создании
            return (clientId, clientSecret);
        }

        /// <summary>
        /// Аутентификация сервисного клиента и выдача токена
        /// </summary>
        public async Task<string> AuthenticateAsync(string clientId, string clientSecret, string ipAddress)
        {
            var client = await _repo.GetByClientIdAsync(clientId);
            if (client == null || !client.IsActive)
                throw new UnauthorizedAccessException("Неверные учётные данные клиента");

            var verify = _hasher.VerifyHashedPassword(client, client.ClientSecretHash, clientSecret);
            if (verify == Microsoft.AspNetCore.Identity.PasswordVerificationResult.Failed)
                throw new UnauthorizedAccessException("Неверные учётные данные клиента");

            client.LastUsedAt = DateTime.UtcNow;
            _repo.Update(client);

            return await _jwt.GenerateAccessTokenForServiceAsync(client);
        }

        public async Task DeactivateAsync(Guid id)
        {
            var client = await _repo.GetByIdAsync(id);
            if (client == null) return;
            client.IsActive = false;
            _repo.Update(client);
        }

        public async Task ActivateAsync(Guid id)
        {
            var client = await _repo.GetByIdAsync(id);
            if (client == null) return;
            client.IsActive = true;
            _repo.Update(client);
        }

        public async Task DeleteAsync(Guid id) => await _repo.DeleteAsync(id);

        public async Task<IReadOnlyList<ServiceClient>> GetAllAsync() => await _repo.GetAllAsync();

        private string GenerateSecret()
        {
            var bytes = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }
    }
}
