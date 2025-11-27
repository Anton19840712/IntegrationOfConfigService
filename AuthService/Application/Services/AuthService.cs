using SipIntegration.EventBus.RabbitMQ.Abstractions;

﻿using Application.DTOs.OTP;
using Application.Interfaces;
using Application.Interfaces.Repository;
using Application.Interfaces.Service;
using Application.ServiceEvents;
using Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Application.Services
{
    public class AuthService(IUserRepository userRepo, IRefreshTokenRepository refreshTokenRepo, IAuditLogRepository auditLogRepo, IPasswordHasher<User> passwordHasher,
		IJwtTokenGenerator jwtTokenGenerator, IEventBus eventBus, IConfiguration configuration, IHttpContextAccessor httpContextAccessor, IUserBehaviorAnalyzer userBehaviorAnalyzer,
		ITotpService totpService, ILogger<AuthService> logger, IDataEncryptor dataEncryptor, ILoginRateLimiter loginRateLimiter)
	{
        private readonly IUserRepository _userRepo = userRepo;
        private readonly IRefreshTokenRepository _refreshTokenRepo = refreshTokenRepo;
        private readonly IAuditLogRepository _auditLogRepo = auditLogRepo;
        private readonly IPasswordHasher<User> _passwordHasher = passwordHasher;
        private readonly IJwtTokenGenerator _jwtTokenGenerator = jwtTokenGenerator;
        private readonly IEventBus _eventBus = eventBus;
        private readonly IConfiguration _configuration = configuration;
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
        private readonly IUserBehaviorAnalyzer _userBehaviorAnalyzer = userBehaviorAnalyzer;
        private readonly ITotpService _totpService = totpService;
        private readonly ILogger<AuthService> _logger = logger;
        private readonly IDataEncryptor _dataEncryptor = dataEncryptor;
        private readonly ILoginRateLimiter _loginRateLimiter = loginRateLimiter;

		public async Task<LoginResponse> LoginAsync(string login, string password, string ipAddress)
        {
            // 1. Проверяем rate limiting ДО любых проверок в БД (защита от brute-force)
            var rateLimitKey = $"{login}:{ipAddress}"; // Комбинированный ключ: логин + IP
            var rateLimitResult = await _loginRateLimiter.CheckAsync(rateLimitKey);

            if (!rateLimitResult.Allowed)
            {
                _logger.LogWarning("Login blocked due to rate limiting for {Login} from {IpAddress}. Retry after {RetryAfter} seconds",
                    login, ipAddress, rateLimitResult.RetryAfterSeconds);

                await LogAudit(null, login, "LOGIN_BLOCKED",
                    $"Попытка входа заблокирована из-за превышения лимита попыток. Повторите через {rateLimitResult.RetryAfterSeconds} секунд",
                    ipAddress);

                throw new UnauthorizedAccessException(
                    $"Слишком много неудачных попыток входа. Повторите попытку через {rateLimitResult.RetryAfterSeconds} секунд.");
            }

            var user = await _userRepo.GetByLoginAsync(login);
            if (user == null || !user.IsActive)
            {
                // Инкрементируем счетчик неудачных попыток
                await _loginRateLimiter.IncrementAsync(rateLimitKey);

                await LogAudit(null, login, "LOGIN_FAILED", $"Неудачная попытка входа: пользователь не найден или заблокирован", ipAddress);
                throw new UnauthorizedAccessException("Неправильный логин или пароль");
            }

            var passwordVerification = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
            if (passwordVerification == Microsoft.AspNetCore.Identity.PasswordVerificationResult.Failed)
            {
                // Инкрементируем счетчик неудачных попыток
                await _loginRateLimiter.IncrementAsync(rateLimitKey);

                await LogAudit(user.Id, login, "LOGIN_FAILED", $"Неудачная попытка входа: неверный пароль", ipAddress);
                throw new UnauthorizedAccessException("Неправильный логин или пароль");
            }

            if (user.IsOtpEnabled)
            {
                // Если 2FA включена, не выдаем токены.
                // Вместо этого возвращаем флаг, что требуется второй фактор.
                return new LoginResponse { IsOtpRequired = true, UserId = user.Id };
            }

            // Успешная аутентификация - сбрасываем счетчик попыток
            await _loginRateLimiter.ResetAsync(rateLimitKey);

            user.LastLoginAt = DateTime.UtcNow;
            user.LastLoginIp = ipAddress;
            _userRepo.Update(user);

            var userAgent = _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString() ?? "unknown";

            var accessToken = await _jwtTokenGenerator.GenerateAccessTokenAsync(user);
            var refreshToken = await _jwtTokenGenerator.GenerateRefreshTokenAsync(user, ipAddress);

            await LogAudit(user.Id, login, "LOGIN_SUCCESS", $"Пользователь успешно вошёл", ipAddress);
            await _userBehaviorAnalyzer.AnalyzeUserLoginAsync(user, ipAddress, userAgent);

            // Отправка события в шину
            await _eventBus.PublishAsync(new UserLoggedInEvent
            {
                UserId = user.Id,
                UserLogin = user.Login,
                IpAddress = ipAddress,
                TimestampUtc = DateTime.UtcNow,
                UserAgent = userAgent
            });

            return new LoginResponse
            {
                IsOtpRequired = false,
                AccessToken = accessToken,
                RefreshToken = refreshToken.Token
            };
        }

        #region Поддержка OTP

        public async Task<LoginResponse> VerifyOtpAndLoginAsync(Guid userId, string otp, string ipAddress)
        {
            var user = await _userRepo.GetByIdAsync(userId);

            // Проверяем, что пользователь существует, активен и у него действительно включен 2FA
            if (user == null || !user.IsActive || !user.IsOtpEnabled || string.IsNullOrEmpty(user.OtpSecretKey))
            {
                throw new UnauthorizedAccessException("OTP validation failed for this user.");
            }

            // 1. Расшифровываем ключ из базы данных
            var decryptedKey = _dataEncryptor.Decrypt(user.OtpSecretKey);

            // 2. Валидируем OTP с помощью расшифрованного ключа
            if (!_totpService.ValidateOtp(decryptedKey, otp))
            {
                // Здесь можно добавить логику аудита неудачной попытки входа
                throw new UnauthorizedAccessException("Invalid OTP code.");
            }

            // 3. Если код верный, завершаем процесс входа
            user.LastLoginAt = DateTime.UtcNow;
            user.LastLoginIp = ipAddress;
            _userRepo.Update(user);
            // Не забываем сохранить изменения в репозитории (если Update не делает этого автоматически)

            // Генерируем токены доступа и обновления
            var accessToken = await _jwtTokenGenerator.GenerateAccessTokenAsync(user);
            var refreshToken = await _jwtTokenGenerator.GenerateRefreshTokenAsync(user, ipAddress);

            var userAgent = _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString() ?? "unknown";
            await LogAudit(user.Id, user.Login, "LOGIN_SUCCESS", $"Пользователь успешно вошёл", ipAddress);
            await _userBehaviorAnalyzer.AnalyzeUserLoginAsync(user, ipAddress, userAgent);

            return new LoginResponse
            {
                IsOtpRequired = false, // Вход завершен
                UserId = user.Id,
                AccessToken = accessToken,
                RefreshToken = refreshToken.Token
            };
        }

        public async Task<(string secretKey, byte[] qrCode)> GenerateOtpSetupAsync(Guid userId)
        {
            var user = await _userRepo.GetByIdAsync(userId) ?? throw new KeyNotFoundException("User not found.");
			var secretKey = _totpService.GenerateSecretKey();
            user.OtpSecretKey = _dataEncryptor.Encrypt(secretKey);         
            _userRepo.Update(user);

            var appName = _configuration["AppName"] ?? "MyApp";
            var uri = _totpService.GenerateQrCodeUri(user.Email!, secretKey, appName);
            var qrCode = _totpService.GenerateQrCode(uri);

            return (secretKey, qrCode);
        }

		public async Task ConfirmOtpSetupAsync(Guid userId, string otp)
		{
			const string logMessageTemplate = "Attempting to confirm OTP for user {UserId} with code {Otp}";
			_logger.LogDebug(logMessageTemplate, userId, otp);

			var user = await _userRepo.GetByIdAsync(userId);
			if (user == null || string.IsNullOrEmpty(user.OtpSecretKey))
			{
				const string warningMessageTemplate = "User {UserId} or their OtpSecretKey not found during OTP confirmation.";
				_logger.LogWarning(warningMessageTemplate, userId);
				throw new InvalidOperationException("OTP setup was not initiated.");
			}

			// 1. Расшифровываем ключ, хранящийся в базе
			var decryptedKey = _dataEncryptor.Decrypt(user.OtpSecretKey);

			// 2. Используем расшифрованный ключ для валидации
			if (!_totpService.ValidateOtp(decryptedKey, otp))
			{
				const string validationFailedMessageTemplate = "OTP validation FAILED for user {UserId}.";
				_logger.LogWarning(validationFailedMessageTemplate, userId);

				// Попытка вычислить ожидаемый код для отладки (необязательно, но полезно)
				try
				{
					var expectedOtp = new OtpNet.Totp(OtpNet.Base32Encoding.ToBytes(decryptedKey)).ComputeTotp();
					const string expectedOtpMessageTemplate = "Server expected OTP around {ExpectedOtp}, but received {Otp}";
					_logger.LogWarning(expectedOtpMessageTemplate, expectedOtp, otp);
				}
				catch (Exception ex)
				{
					const string errorMessageTemplate = "Error while computing expected OTP for debugging.";
					_logger.LogError(ex, errorMessageTemplate);
				}

				throw new InvalidOperationException("Invalid OTP code. Please try again.");
			}

			const string validationSucceededMessageTemplate = "OTP validation SUCCEEDED for user {UserId}.";
			_logger.LogDebug(validationSucceededMessageTemplate, userId);

			// 3. Если код верный, включаем 2FA для пользователя
			user.IsOtpEnabled = true;
			user.OtpEnabledAt = DateTime.UtcNow;
			_userRepo.Update(user);
		}
       
        public async Task DisableOtpAsync(Guid userId)
        {
            var user = await _userRepo.GetByIdAsync(userId) ?? throw new KeyNotFoundException("User not found.");
			user.IsOtpEnabled = false;
            user.OtpSecretKey = null;
            user.OtpEnabledAt = null;
            _userRepo.Update(user);
        }

        #endregion

        public async Task<(string AccessToken, string RefreshToken)> RefreshTokenAsync(string token, string ipAddress)
        {
            var refreshToken = await _refreshTokenRepo.GetByTokenAsync(token);

            // Сценарий 1: Токен не найден в базе.
            if (refreshToken == null)
            {
                await LogAudit(null, null, "REFRESH_FAILED_NOT_FOUND", "Попытка использования несуществующего refresh токена", ipAddress);
                throw new UnauthorizedAccessException("Refresh токен недействителен");
            }

            // Сценарий 2: Токен отозван. Это путь для Grace Period.
            if (refreshToken.IsRevoked)
            {
                // Проверяем, на какой токен он был заменен.
                var replacementTokenString = refreshToken.ReplacedByToken;
                if (string.IsNullOrEmpty(replacementTokenString))
                {
                    // Токен отозван, но не заменен (например, при выходе из системы). Доступ запрещен.
                    await LogAudit(refreshToken.UserId, null, "REFRESH_FAILED_REVOKED", "Использован отозванный refresh токен без замены.", ipAddress);
                    throw new UnauthorizedAccessException("Refresh токен недействителен");
                }

                // Ищем в базе токен, который пришел на замену старому.
                var replacementToken = await _refreshTokenRepo.GetByTokenAsync(replacementTokenString);

                // Проверяем, что заменяющий токен существует и активен, и что мы находимся в пределах Grace Period.
                var gracePeriodSeconds = _configuration.GetValue<int>("JwtSettings:RefreshTokenGracePeriodSeconds", 60);
                var gracePeriodEndsAt = refreshToken.RevokedAt?.AddSeconds(gracePeriodSeconds);

                if (replacementToken == null || replacementToken.IsRevoked || DateTime.UtcNow > gracePeriodEndsAt)
                {
                    // Если заменяющий токен тоже отозван или льготный период истек,
                    // это может быть атакой. Отказываем в доступе.
                    await LogAudit(refreshToken.UserId, null, "REFRESH_FAILED_REPLAY_ATTACK", "Попытка повторного использования токена вне льготного периода.", ipAddress);
                    throw new UnauthorizedAccessException("Refresh токен недействителен");
                }

                // Если все проверки пройдены, это легитимный случай race condition.
                // Выдаем новый access token, но возвращаем тот же самый (уже выданный) refresh token.
                var userForGracePeriod = await _userRepo.GetByIdAsync(replacementToken.UserId);
                var newAccessToken = await _jwtTokenGenerator.GenerateAccessTokenAsync(userForGracePeriod);
                
                await LogAudit(userForGracePeriod.Id, userForGracePeriod.Login, "REFRESH_GRACE_SUCCESS", "Успешное обновление токена в льготный период", ipAddress);
                
                return (newAccessToken, replacementToken.Token);
            }
            
            // Сценарий 3: Токен истек.
            if (refreshToken.IsExpired)
            {
                await LogAudit(refreshToken.UserId, null, "REFRESH_FAILED_EXPIRED", "Использован просроченный refresh токен.", ipAddress);
                throw new UnauthorizedAccessException("Refresh токен недействителен");
            }

            // Сценарий 4: Нормальное обновление токена.
            // Токен валиден, не отозван и не просрочен.
            var user = await _userRepo.GetByIdAsync(refreshToken.UserId);
            if (user == null || !user.IsActive)
            {
                await LogAudit(refreshToken.UserId, user?.Login, "REFRESH_FAILED", "Пользователь не найден или заблокирован", ipAddress);
                throw new UnauthorizedAccessException("Пользователь недействителен");
            }

            // Генерируем совершенно новую пару токенов.
            var finalAccessToken = await _jwtTokenGenerator.GenerateAccessTokenAsync(user);
            var finalRefreshToken = await _jwtTokenGenerator.GenerateRefreshTokenAsync(user, ipAddress);

            // Отзываем старый токен и связываем его с новым.
            refreshToken.RevokedAt = DateTime.UtcNow;
            refreshToken.RevokedByIp = ipAddress;
            refreshToken.ReplacedByToken = finalRefreshToken.Token; // Связываем старый с новым
            await _refreshTokenRepo.UpdateAsync(refreshToken);

            await LogAudit(user.Id, user.Login, "REFRESH_SUCCESS", "Refresh токен успешно обновлён", ipAddress);

            return (finalAccessToken, finalRefreshToken.Token);
        }

        public async Task RevokeRefreshTokenAsync(string token, string ipAddress)
        {
            var refreshToken = await _refreshTokenRepo.GetByTokenAsync(token);
            if (refreshToken == null || refreshToken.IsRevoked)
            {
                return; // Уже отозван или не существует
            }

            refreshToken.RevokedAt = DateTime.UtcNow;
            refreshToken.RevokedByIp = ipAddress;
            await _refreshTokenRepo.UpdateAsync(refreshToken);

            await LogAudit(refreshToken.UserId, null, "TOKEN_REVOKED", "Refresh токен отозван", ipAddress);
        }

        public async Task LogoutAsync(string token, string ipAddress, Guid? currentUserId, string currentUserLogin)
        {
            var existing = await _refreshTokenRepo.GetByTokenAsync(token);
            if (existing == null || existing.IsRevoked)
                return;

            existing.RevokedAt = DateTime.UtcNow;
            existing.RevokedByIp = ipAddress;
            await _refreshTokenRepo.UpdateAsync(existing);

            await LogAudit(currentUserId ?? existing.UserId, currentUserLogin ?? "system", "LOGOUT", "Пользователь вышел из системы", ipAddress);
            
            // Отправка события в шину
            await _eventBus.PublishAsync(new UserLoggedOutEvent
            {
                UserId = existing.Id,
                IpAddress = ipAddress
            });
        }

        private async Task LogAudit(Guid? userId, string userLogin, string action, string description, string ip)
        {
            var log = new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                UserLogin = userLogin,
                Action = action,
                Description = description,
                Timestamp = DateTime.UtcNow,
                IpAddress = ip
            };
            await _auditLogRepo.AddAsync(log);
        }
    }
}
