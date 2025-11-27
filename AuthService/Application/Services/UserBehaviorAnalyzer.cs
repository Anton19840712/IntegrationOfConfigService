using Application.Interfaces.Repository;
using Application.Interfaces.Service;
using Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Application.Services
{
    public class UserBehaviorAnalyzer : IUserBehaviorAnalyzer
    {
        private readonly IUserBehaviorRepository _repository;
        private readonly ILogger<UserBehaviorAnalyzer> _logger;

        public UserBehaviorAnalyzer(IUserBehaviorRepository repository, ILogger<UserBehaviorAnalyzer> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task AnalyzeUserLoginAsync(User user, string ipAddress, string userAgent)
        {
            if (user == null) return;

            var profile = await _repository.GetByUserIdAsync(user.Id);

            if (profile == null)
            {
                profile = new UserBehaviorProfile
                {
                    UserId = user.Id,
                    KnownIpAddresses = new List<string> { ipAddress },
                    KnownUserAgents = new List<string> { userAgent },
                    TypicalActiveHoursUtc = new List<int> { DateTime.UtcNow.Hour },
                    LastUpdatedAt = DateTime.UtcNow
                };
                await _repository.AddAsync(profile);
                _logger.LogInformation("Создан новый профиль поведения для пользователя {UserId}", user.Id);
                return;
            }

            int riskScore = 0;
            bool isNewIp = !profile.KnownIpAddresses.Contains(ipAddress);
            bool isNewAgent = !profile.KnownUserAgents.Contains(userAgent);
            bool isUnusualHour = !profile.TypicalActiveHoursUtc.Contains(DateTime.UtcNow.Hour);

            if (isNewIp) riskScore += 10;
            if (isNewAgent) riskScore += 5;
            if (isUnusualHour) riskScore += 10;

            _logger.LogInformation("Вход пользователя {UserId} (IP: {Ip}, Agent: {Agent}), RiskScore={RiskScore}",
                user.Id, ipAddress, userAgent, riskScore);

            if (riskScore >= 25)
            {
                _logger.LogWarning("Обнаружена подозрительная активность для пользователя {UserId} (RiskScore={RiskScore})", user.Id, riskScore);
                // Здесь можно добавить логику отзыва токенов и уведомления
            }

            if (isNewIp)
            {
                profile.KnownIpAddresses.Add(ipAddress);
                if (profile.KnownIpAddresses.Count > 20)
                    profile.KnownIpAddresses = profile.KnownIpAddresses.Skip(profile.KnownIpAddresses.Count - 20).ToList();
            }
            if (isNewAgent)
            {
                profile.KnownUserAgents.Add(userAgent);
                if (profile.KnownUserAgents.Count > 20)
                    profile.KnownUserAgents = profile.KnownUserAgents.Skip(profile.KnownUserAgents.Count - 20).ToList();
            }
            if (isUnusualHour)
            {
                profile.TypicalActiveHoursUtc.Add(DateTime.UtcNow.Hour);
                if (profile.TypicalActiveHoursUtc.Count > 24)
                    profile.TypicalActiveHoursUtc = profile.TypicalActiveHoursUtc.Skip(profile.TypicalActiveHoursUtc.Count - 24).ToList();
            }

            profile.LastUpdatedAt = DateTime.UtcNow;
            await _repository.UpdateAsync(profile);
        }
    }
}
