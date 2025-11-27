using Application.DTOs.Users;
using Domain.Entities;

namespace Application.Interfaces.Service
{
    public interface IUserService
    {
        Task<IEnumerable<User>> GetAllAsync();
        Task<User> GetByIdAsync(Guid id);
        Task<User> GetByIdWithRolesAsync(Guid id); // Метод для получения пользователя с ролями
        Task<User> CreateUserAsync(CreateUserDto dto, Guid? createdByUserId = null);
        Task UpdateUserAsync(Guid userId, UpdateUserDto dto, Guid? updatedByUserId = null);
        Task UpdateUserRolesAsync(Guid userId, List<Guid> roleIds, Guid? adminUserId = null);
        Task ChangePasswordAsync(Guid userId, ChangeUserPasswordDto dto, Guid? adminUserId = null);
        Task SetUserStatusAsync(Guid userId, bool isActive, Guid? adminUserId = null);
    }
}
