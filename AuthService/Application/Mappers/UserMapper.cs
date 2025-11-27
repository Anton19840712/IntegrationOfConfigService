using Application.DTOs.Roles;
using Application.DTOs.Users;
using Domain.Entities;

namespace Application.Mappers
{
    public static class UserMapper
    {
        public static UserDto ToDto(this User user)
        {
            var roles = user.UserRoles?.Select(ur => ur.Role.ToDto()).ToList() ?? new List<RoleDto>();
            var privileges = roles.SelectMany(r => r.Privileges).Select(p => p.Name).Distinct().ToList();

            return new UserDto
            {
                Id = user.Id,
                Login = user.Login,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                MiddleName = user.MiddleName,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                Roles = roles,
                Privileges = privileges
            };
        }
    }
}
