using SipIntegration.EventBus.RabbitMQ.Abstractions;

﻿using Application.DTOs.Users;
using Application.Interfaces.Repository;
using Application.Interfaces.Service;
using Application.ServiceEvents;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using PasswordVerificationResult = Microsoft.AspNetCore.Identity.PasswordVerificationResult;

namespace Application.Services
{
    public class UserService(
		IUserRepository userRepo,
		IUserRoleRepository userRoleRepo,
		IRoleRepository roleRepo,
		IAuditLogRepository auditLogRepo,
		IEventBus eventBus,
		IPasswordHasher<User> passwordHasher) : IUserService
    {
        private readonly IUserRepository _userRepo = userRepo;
        private readonly IUserRoleRepository _userRoleRepo = userRoleRepo;
        private readonly IRoleRepository _roleRepo = roleRepo;
        private readonly IAuditLogRepository _auditLogRepo = auditLogRepo;
        private readonly IEventBus _eventBus = eventBus;
        private readonly IPasswordHasher<User> _passwordHasher = passwordHasher;

		public async Task<IEnumerable<User>> GetAllAsync() => await _userRepo.GetAllAsync();
        public async Task<User> GetByIdAsync(Guid id) => await _userRepo.GetByIdAsync(id);
        public async Task<User> GetByIdWithRolesAsync(Guid id) => await _userRepo.GetByIdWithPreviegesAsync(id);

		public async Task<User> CreateUserAsync(CreateUserDto dto, Guid? createdByUserId = null)
		{
			// Проверка на уникальность логина и email
			if (await _userRepo.AnyWithLoginOrEmailAsync(dto.Login, dto.Email))
				throw new InvalidOperationException("Пользователь с таким логином или email уже существует.");

			var user = new User
			{
				Id = Guid.NewGuid(),
				Login = dto.Login,
				Email = dto.Email,
				FirstName = dto.FirstName,
				LastName = dto.LastName,
				MiddleName = dto.MiddleName,
				IsActive = true,
				CreatedAt = DateTime.UtcNow
			};

			// Хеширование пароля должно происходить здесь, в сервисе
			user.PasswordHash = _passwordHasher.HashPassword(user, dto.Password);

			await _userRepo.AddAsync(user);

			if (dto.RoleIds.Count > 0) // Replaced Any() with Count > 0
			{
				await UpdateUserRolesAsync(user.Id, dto.RoleIds, createdByUserId);
			}

			await _auditLogRepo.AddAsync(new AuditLog
			{
				Id = Guid.NewGuid(),
				UserId = createdByUserId,
				UserLogin = null,
				Action = "USER_CREATE",
				Description = $"Создан пользователь {user.Login}",
				Timestamp = DateTime.UtcNow
			});

			await _eventBus.PublishAsync(new UserCreatedEvent
			{
				UserId = user.Id,
				UserLogin = user.Login,
				Email = user.Email,
				FirstName = user.FirstName,
				LastName = user.LastName,
				MiddleName = user.MiddleName
			});

			return user;
		}

        public async Task UpdateUserAsync(Guid userId, UpdateUserDto dto, Guid? updatedByUserId = null)
        {
            var user = await _userRepo.GetByIdAsync(userId) ?? throw new KeyNotFoundException("Пользователь не найден.");
			bool hasChanges = false;
            var changesDescription = new List<string>();

            // Проверяем и обновляем каждое поле
            if (dto.Email != null && user.Email != dto.Email)
            {
                // Дополнительная проверка на уникальность нового email
                if (await _userRepo.GetByEmailAsync(dto.Email) != null)
                {
                    throw new InvalidOperationException($"Email '{dto.Email}' уже занят.");
                }
                changesDescription.Add($"Email изменен с '{user.Email}' на '{dto.Email}'");
                user.Email = dto.Email;
                hasChanges = true;
            }

            if (dto.FirstName != null && user.FirstName != dto.FirstName)
            {
                changesDescription.Add("Изменено имя");
                user.FirstName = dto.FirstName;
                hasChanges = true;
            }

            if (dto.LastName != null && user.LastName != dto.LastName)
            {
                changesDescription.Add("Изменено фамилия");
                user.LastName = dto.LastName;
                hasChanges = true;
            }

            if (dto.MiddleName != null && user.MiddleName != dto.MiddleName)
            {
                changesDescription.Add("Изменено отчество");
                user.MiddleName = dto.MiddleName;
                hasChanges = true;
            }

            // Если никаких изменений не было, просто выходим
            if (!hasChanges)
            {
                return;
            }

            _userRepo.Update(user);

            await _auditLogRepo.AddAsync(new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = updatedByUserId,
                UserLogin = null,
                Action = "USER_PROFILE_UPDATE",
                Description = $"Обновлен профиль пользователя '{user.Login}': {string.Join(", ", changesDescription)}",
                Timestamp = DateTime.UtcNow
            });

            await _eventBus.PublishAsync(new UserUpdatedEvent
            {
                UserId = user.Id,
                UserLogin = user.Login,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                MiddleName = user.MiddleName
            });
        }

        public async Task UpdateUserRolesAsync(Guid userId, List<Guid> newRoleIds, Guid? adminUserId = null)
        {
            var currentUserRoles = await _userRoleRepo.GetRolesForUserAsync(userId);
            var currentRoleIds = currentUserRoles.Select(ur => ur.RoleId).ToList();

            var rolesToRemove = currentUserRoles.Where(ur => !newRoleIds.Contains(ur.RoleId)).ToList();
            var roleIdsToAdd = newRoleIds.Where(id => !currentRoleIds.Contains(id)).ToList();

            if (rolesToRemove.Count > 0) _userRoleRepo.DeleteRange(rolesToRemove);

            if (roleIdsToAdd.Count > 0)
            {
                var newUserRoles = roleIdsToAdd.Select(roleId => new UserRole { UserId = userId, RoleId = roleId });
                await _userRoleRepo.AddRangeAsync(newUserRoles);
            }

            if (rolesToRemove.Count > 0 || roleIdsToAdd.Count > 0)
            {
                await _auditLogRepo.AddAsync(new AuditLog
                {
                    UserId = adminUserId,
                    Action = "USER_ROLES_UPDATE",
                    Description = $"Обновлены роли для пользователя с ID {userId}",
                    Timestamp = DateTime.UtcNow
                });

                await _eventBus.PublishAsync(new UserRoleChangeEvent
                {
                    UserId = userId,
                    NewRoleIds = newRoleIds,
                    RemoveRoleIds = [.. rolesToRemove.Select(x => x.RoleId)]
				});
            }
        }

        public async Task ChangePasswordAsync(Guid userId, ChangeUserPasswordDto dto, Guid? adminUserId = null)
        {
            var user = await _userRepo.GetByIdAsync(userId) ?? throw new KeyNotFoundException("Пользователь не найден.");

            var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.OldPassword);
            if (verificationResult == PasswordVerificationResult.Failed)
            {
                throw new InvalidOperationException("Старый пароль неверен.");
            }
            user.PasswordHash = _passwordHasher.HashPassword(user, dto.NewPassword);
            
            _userRepo.Update(user);

            await _auditLogRepo.AddAsync(new AuditLog
            {
                UserId = adminUserId ?? userId,
                Action = "USER_PASSWORD_CHANGE",
                Description = $"Пароль для пользователя '{user.Login}' (ID: {user.Id}) был изменен.",
                Timestamp = DateTime.UtcNow
            });
        }

        public async Task SetUserStatusAsync(Guid userId, bool isActive, Guid? adminUserId = null)
        {
            var user = await _userRepo.GetByIdAsync(userId) ?? throw new KeyNotFoundException("Пользователь не найден.");

            if (user.IsActive == isActive) return; // Статус не изменился

            user.IsActive = isActive;
            _userRepo.Update(user);

            await _auditLogRepo.AddAsync(new AuditLog
            {
                UserId = adminUserId,
                Action = isActive ? "USER_UNBLOCKED" : "USER_BLOCKED",
                Description = $"Пользователь '{user.Login}' (ID: {user.Id}) был {(isActive ? "разблокирован" : "заблокирован")}.",
                Timestamp = DateTime.UtcNow
            });

            if (isActive)
            {
                await _eventBus.PublishAsync(new UserUnblockedEvent
                {
                    UserId = userId,
                    UserLogin = user.Login,
                });
            }
            else
            {
                await _eventBus.PublishAsync(new UserBlockedEvent
                {
                    UserId = userId,
                    UserLogin = user.Login,
                });
            }
        }

    }
}
