using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data
{
    public class AuthDbSeeder
    {
        private readonly AuthDbContext _db;
        private readonly IPasswordHasher<User> _passwordHasher;

        public AuthDbSeeder(AuthDbContext db, IPasswordHasher<User> passwordHasher)
        {
            _db = db;
            _passwordHasher = passwordHasher;
        }

        public async Task SeedAsync()
        {
            // Создаём базу и прогоняем миграции
            //await _db.Database.MigrateAsync();

            // Создаём роли
            var adminRole = await CreateRoleIfNotExistsAsync("Admin");
            var userRole = await CreateRoleIfNotExistsAsync("User");

            // Создаём базовые привилегии
            var allPrivileges = new List<Privilege>
            {
                await CreatePrivilegeIfNotExistsAsync("ViewUsers"),
                await CreatePrivilegeIfNotExistsAsync("ManageUsers"),
                await CreatePrivilegeIfNotExistsAsync("ViewRoles"),
                await CreatePrivilegeIfNotExistsAsync("ManageRoles"),
                await CreatePrivilegeIfNotExistsAsync("ManageTokens"),
                await CreatePrivilegeIfNotExistsAsync("AccessInternalApi")
                // Добавляйте любые другие нужные привилегии
            };

            // Привязываем все привилегии к роли Admin
            foreach (var privilege in allPrivileges)
            {
                await AssignPrivilegeToRoleIfNotAssigned(adminRole, privilege);
            }

            // Привязываем минимальный набор привилегий к роли User (пример)
            var userPrivileges = new[]
            {
                "ViewUsers",
                "AccessInternalApi"
            };
            foreach (var privName in userPrivileges)
            {
                var privilege = allPrivileges.Find(p => p.Name == privName);
                if (privilege != null)
                {
                    await AssignPrivilegeToRoleIfNotAssigned(userRole, privilege);
                }
            }

            // Создаём пользователей
            var adminUser = await CreateUserIfNotExistsAsync(
                login: "admin",
                password: "admin",
                firstName: "Системный",
                lastName: "Администратор",
                middleName: "",
                email: "admin@example.com",
                role: adminRole
            );

            var simpleUser = await CreateUserIfNotExistsAsync(
                login: "user",
                password: "user",
                firstName: "Обычный",
                lastName: "Пользователь",
                middleName: "",
                email: "user@example.com",
                role: userRole
            );
        }

        private async Task<Role> CreateRoleIfNotExistsAsync(string name)
        {
            var role = await _db.Roles.FirstOrDefaultAsync(r => r.Name == name);
            if (role == null)
            {
                role = new Role
                {
                    Id = Guid.NewGuid(),
                    Name = name
                };
                await _db.Roles.AddAsync(role);
                await _db.SaveChangesAsync();
            }
            return role;
        }

        private async Task<Privilege> CreatePrivilegeIfNotExistsAsync(string name)
        {
            var privilege = await _db.Privileges.FirstOrDefaultAsync(p => p.Name == name);
            if (privilege == null)
            {
                privilege = new Privilege
                {
                    Id = Guid.NewGuid(),
                    Name = name
                };
                await _db.Privileges.AddAsync(privilege);
                await _db.SaveChangesAsync();
            }
            return privilege;
        }

        private async Task AssignPrivilegeToRoleIfNotAssigned(Role role, Privilege privilege)
        {
            var exists = await _db.RolePrivileges.AnyAsync(rp => rp.RoleId == role.Id && rp.PrivilegeId == privilege.Id);
            if (!exists)
            {
                var rolePrivilege = new RolePrivilege
                {
                    RoleId = role.Id,
                    PrivilegeId = privilege.Id
                };
                await _db.RolePrivileges.AddAsync(rolePrivilege);
                await _db.SaveChangesAsync();
            }
        }

        private async Task<User> CreateUserIfNotExistsAsync(
            string login,
            string password,
            string firstName,
            string lastName,
            string middleName,
            string email,
            Role role
        )
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Login == login);
            if (user == null)
            {
                user = new User
                {
                    Id = Guid.NewGuid(),
                    Login = login,
                    FirstName = firstName,
                    LastName = lastName,
                    MiddleName = middleName,
                    Email = email,
                    CreatedAt = DateTime.UtcNow,
                    LastLoginIp = "",
                    IsActive = true
                };

                user.PasswordHash = _passwordHasher.HashPassword(user, password);

                await _db.Users.AddAsync(user);
                await _db.SaveChangesAsync();

                // Привязываем роль к пользователю
                var userRole = new UserRole
                {
                    UserId = user.Id,
                    RoleId = role.Id
                };
                await _db.UserRoles.AddAsync(userRole);
                await _db.SaveChangesAsync();
            }
            return user;
        }
    }
}
