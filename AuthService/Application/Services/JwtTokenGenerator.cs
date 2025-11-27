using Application.Interfaces.Repository;
using Application.Interfaces.Service;
using Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Application.Services
{
    public class JwtTokenGenerator : IJwtTokenGenerator
    {
        private readonly IConfiguration _configuration;
        private readonly IRefreshTokenRepository _refreshTokenRepo;

        public JwtTokenGenerator(IConfiguration configuration, IRefreshTokenRepository refreshTokenRepo)
        {
            _configuration = configuration;
            _refreshTokenRepo = refreshTokenRepo;
        }

        public Task<string> GenerateAccessTokenAsync(User user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var expires = DateTime.UtcNow.AddMinutes(double.Parse(jwtSettings["AccessTokenExpirationMinutes"]));

            // Собираем claims для JWT
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.Login),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
                new Claim("fullname", $"{user.LastName} {user.FirstName} {user.MiddleName}".Trim())
            };

            // Добавляем роли и привилегии (scopes)
            foreach (var userRole in user.UserRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, userRole.Role.Name));

                if (userRole.Role.RolePrivileges != null)
                {
                    foreach (var rolePrivilege in userRole.Role.RolePrivileges)
                    {
                        claims.Add(new Claim("scope", rolePrivilege.Privilege.Name));
                    }
                }
            }

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            return Task.FromResult(tokenString);
        }

        public async Task<RefreshToken> GenerateRefreshTokenAsync(User user, string ipAddress)
        {
            var randomBytes = new byte[64];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }
            var refreshToken = Convert.ToBase64String(randomBytes);

            // Обычно срок действия refresh токена больше, например 7-30 дней
            var refreshTokenExpiryDays = int.Parse(_configuration["JwtSettings:RefreshTokenExpirationDays"]);

            // Создаём новый RefreshToken объект для сохранения в базе
            var refreshTokenEntity = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = refreshToken,
                Expires = DateTime.UtcNow.AddDays(refreshTokenExpiryDays), // срок действия
                CreatedAt = DateTime.UtcNow,
                CreatedByIp = ipAddress
            };

            await _refreshTokenRepo.AddAsync(refreshTokenEntity);

            return refreshTokenEntity;
        }


        public Task<string> GenerateAccessTokenForServiceAsync(ServiceClient client)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var expires = DateTime.UtcNow.AddMinutes(double.Parse(jwtSettings["AccessTokenExpirationMinutes"]));

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, client.Id.ToString()),
                new Claim("client_id", client.ClientId),
                new Claim("scope", "internal_access")
            };

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            

            return Task.FromResult(new JwtSecurityTokenHandler().WriteToken(token));
        }
    }
}
