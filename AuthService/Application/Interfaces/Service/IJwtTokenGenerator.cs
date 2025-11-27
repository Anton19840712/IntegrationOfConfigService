using Domain.Entities;

namespace Application.Interfaces.Service
{
    public interface IJwtTokenGenerator
    {
        Task<string> GenerateAccessTokenAsync(User user);
        Task<RefreshToken> GenerateRefreshTokenAsync(User user, string ipAddress);
        Task<string> GenerateAccessTokenForServiceAsync(ServiceClient client);
    }
}
