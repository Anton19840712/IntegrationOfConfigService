using SipIntegration.EventBus.RabbitMQ.Abstractions;

﻿using Application.Interfaces.Repository;
using Application.Interfaces.Service;
using Domain.Entities;

namespace Application.Services
{
    public class RoleService(
		IRoleRepository roleRepo,
		IRolePrivilegeRepository rolePrivilegeRepo,
		IPrivilegeRepository privilegeRepo,
		IAuditLogRepository auditLogRepo,
		IEventBus eventBus)
	{
        private readonly IRoleRepository _roleRepo = roleRepo;
        private readonly IRolePrivilegeRepository _rolePrivilegeRepo = rolePrivilegeRepo;
        private readonly IPrivilegeRepository _privilegeRepo = privilegeRepo;
        private readonly IAuditLogRepository _auditLogRepo = auditLogRepo;
        private readonly IEventBus _eventBus = eventBus;

		public async Task<IReadOnlyList<Role>> GetAllRolesAsync()
        {
            return await _roleRepo.GetAllAsync();
        }

        public async Task<Role> GetRoleByIdAsync(Guid id)
        {
            return await _roleRepo.GetByIdAsync(id);
        }

        public async Task<Role> CreateRoleAsync(string name, List<Guid> privilegeIds, Guid? adminUserId = null)
        {
            var existing = await _roleRepo.GetByNameAsync(name);
            if (existing != null)
                throw new InvalidOperationException($"Роль с именем '{name}' уже существует.");

            var role = new Role
            {
                Id = Guid.NewGuid(),
                Name = name
            };

            await _roleRepo.AddAsync(role);

            foreach (var privilegeId in privilegeIds)
            {
                await _rolePrivilegeRepo.AddAsync(new RolePrivilege
                {
                    RoleId = role.Id,
                    PrivilegeId = privilegeId
                });
            }

            await _auditLogRepo.AddAsync(new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = adminUserId,
                Action = "ROLE_CREATE",
                Description = $"Создана роль {name}",
                Timestamp = DateTime.UtcNow
            });

            var createdRoleWithPrivileges = await _roleRepo.GetByIdWithPrivilegesAsync(role.Id);

            return createdRoleWithPrivileges ?? role; ;
        }

        public async Task UpdateRoleNameAsync(Role role, string newName, Guid? adminUserId = null)
        {
            if (!string.Equals(role.Name, newName, StringComparison.OrdinalIgnoreCase))
            {
                var existing = await _roleRepo.GetByNameAsync(newName);
                if (existing != null)
                {
                    throw new InvalidOperationException($"Роль с именем '{newName}' уже существует.");
                }
            }

            string oldName = role.Name;
            role.Name = newName;

            _roleRepo.Update(role); 

            await _auditLogRepo.AddAsync(new AuditLog
            {
                UserId = adminUserId,
                Action = "ROLE_NAME_UPDATE",
                Description = $"Имя роли '{oldName}' (ID: {role.Id}) изменено на '{newName}'",
                Timestamp = DateTime.UtcNow
            });
        }

		public async Task UpdateRolePrivilegesAsync(Guid roleId, List<Guid> newPrivilegeIds, Guid? adminUserId = null)
		{
			// 1. Получаем все существующие связи для этой роли
			var currentPrivileges = await _rolePrivilegeRepo.GetPrivilegesForRoleAsync(roleId);
			var currentPrivilegeIds = currentPrivileges.Select(rp => rp.PrivilegeId).ToList();

			// 2. Вычисляем, что нужно удалить, а что добавить
			var privilegesToRemove = currentPrivileges
				.Where(rp => !newPrivilegeIds.Contains(rp.PrivilegeId))
				.ToList();

			var privilegeIdsToAdd = newPrivilegeIds
				.Where(id => !currentPrivilegeIds.Contains(id))
				.ToList();

			// 3. Удаляем лишние связи
			if (privilegesToRemove.Count > 0) // Fix for CA1860
			{
				_rolePrivilegeRepo.DeleteRange(privilegesToRemove);
			}

			// 4. Добавляем новые связи
			if (privilegeIdsToAdd.Count > 0) // Fix for CA1860
			{
				var newRolePrivileges = privilegeIdsToAdd.Select(privId => new RolePrivilege
				{
					RoleId = roleId,
					PrivilegeId = privId
				}).ToList();

				await _rolePrivilegeRepo.AddRangeAsync(newRolePrivileges);
			}

			// 5. Логируем, только если были изменения
			if (privilegesToRemove.Count > 0 || privilegeIdsToAdd.Count > 0) // Fix for CA1860
			{
				await _auditLogRepo.AddAsync(new AuditLog
				{
					UserId = adminUserId,
					Action = "ROLE_PRIVILEGES_UPDATE",
					Description = $"Обновлены привилегии для роли с ID {roleId}.",
					Timestamp = DateTime.UtcNow
				});
			}
		}


        public void UpdateRole(Role role)
        {
            _roleRepo.Update(role);
        }

        public void DeleteRole(Role role)
        {
            _roleRepo.Delete(role);
        }

        public async Task AssignPrivilegeAsync(Guid roleId, Guid privilegeId, Guid? adminUserId = null)
        {
            var existing = await _rolePrivilegeRepo.GetAsync(roleId, privilegeId);
            if (existing != null) return;

            await _rolePrivilegeRepo.AddAsync(new RolePrivilege
            {
                RoleId = roleId,
                PrivilegeId = privilegeId
            });

            await _auditLogRepo.AddAsync(new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = adminUserId,
                Action = "PRIVILEGE_ASSIGNED",
                Description = $"Привилегия {privilegeId} назначена роли {roleId}",
                Timestamp = DateTime.UtcNow
            });

        }

        public async Task RemovePrivilegeAsync(Guid roleId, Guid privilegeId, Guid? adminUserId = null)
        {
            var existing = await _rolePrivilegeRepo.GetAsync(roleId, privilegeId);
            if (existing == null) return;

            _rolePrivilegeRepo.Delete(existing);

            await _auditLogRepo.AddAsync(new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = adminUserId,
                Action = "PRIVILEGE_REMOVED",
                Description = $"Привилегия {privilegeId} удалена из роли {roleId}",
                Timestamp = DateTime.UtcNow
            });
        }

        public async Task<IReadOnlyList<Role>> GetAllRolesWithPrivilegesAsync()
        {
            return await _roleRepo.GetAllWithPrivilegesAsync();
        }

        public async Task<Role> GetRoleByIdWithPrivilegesAsync(Guid id)
        {
            return await _roleRepo.GetByIdWithPrivilegesAsync(id);
        }

    }
}
