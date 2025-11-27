using Application.DTOs.Privileges;
using Application.DTOs.Roles;
using Domain.Entities;

namespace Application.Mappers
{
    public static class RoleMapper
    {
        public static RoleDto ToDto(this Role role)
        {
            return new RoleDto
            {
                Id = role.Id,
                Name = role.Name,
                Privileges = role.RolePrivileges?
                    .Select(rp => new PrivilegeDto
                    {
                        Id = rp.Privilege.Id,
                        Name = rp.Privilege.Name
                    }).ToList() ?? new List<PrivilegeDto>()
            };
        }
    }
}
