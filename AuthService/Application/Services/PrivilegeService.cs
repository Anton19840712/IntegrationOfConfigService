using SipIntegration.EventBus.RabbitMQ.Abstractions;

﻿using Application.Interfaces.Repository;
using Application.Interfaces.Service;
using Domain.Entities;

namespace Application.Services
{
    public class PrivilegeService
    {
        private readonly IPrivilegeRepository _privilegeRepo;
        private readonly IAuditLogRepository _auditLogRepo;
        private readonly IEventBus _eventBus;

        public PrivilegeService(IPrivilegeRepository privilegeRepo, IAuditLogRepository auditLogRepo, IEventBus eventBus)
        {
            _privilegeRepo = privilegeRepo;
            _auditLogRepo = auditLogRepo;
            _eventBus = eventBus;
        }

        public async Task<IReadOnlyList<Privilege>> GetAllPrivilegesAsync()
        {
            return await _privilegeRepo.GetAllAsync();
        }

        public async Task<Privilege> GetPrivilegeByIdAsync(Guid id)
        {
            return await _privilegeRepo.GetByIdAsync(id);
        }

        public async Task<Privilege> CreatePrivilegeAsync(string name, Guid? adminUserId = null)
        {
            var existing = await _privilegeRepo.GetByNameAsync(name); // Предположим, есть такой метод
            if (existing != null)
            {
                throw new InvalidOperationException($"Привилегия с именем '{name}' уже существует.");
            }

            var privilege = new Privilege
            {
                Id = Guid.NewGuid(),
                Name = name
            };

            await _privilegeRepo.AddAsync(privilege);

            await _auditLogRepo.AddAsync(new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = adminUserId,
                Action = "PRIVILEGE_CREATE",
                Description = $"Создана привилегия {name}",
                Timestamp = DateTime.UtcNow
            });

            return privilege;
        }

        public async Task UpdatePrivilegeAsync(Privilege privilege, string newName, Guid? adminUserId = null)
        {
            string oldName = privilege.Name;
            privilege.Name = newName;

            _privilegeRepo.Update(privilege);

            await _auditLogRepo.AddAsync(new AuditLog
            {
                UserId = adminUserId,
                Action = "PRIVILEGE_UPDATE",
                Description = $"Имя привилегии изменено с '{oldName}' на '{newName}'",
                Timestamp = DateTime.UtcNow
            });
        }

        public async Task DeletePrivilegeAsync(Privilege privilege, Guid? adminUserId = null)
        {
            _privilegeRepo.Delete(privilege);

            await _auditLogRepo.AddAsync(new AuditLog
            {
                UserId = adminUserId,
                Action = "PRIVILEGE_DELETE",
                Description = $"Удалена привилегия '{privilege.Name}' (ID: {privilege.Id})",
                Timestamp = DateTime.UtcNow
            });
        }
    }
}
