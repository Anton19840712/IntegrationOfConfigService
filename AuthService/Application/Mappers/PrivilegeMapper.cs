using Application.DTOs.Privileges;
using Domain.Entities;

namespace Application.Mappers
{
    public static class PrivilegeMapper
    {
        public static PrivilegeDto ToDto(this Privilege privilege)
        {
            return new PrivilegeDto
            {
                Id = privilege.Id,
                Name = privilege.Name
            };
        }
    }
}
