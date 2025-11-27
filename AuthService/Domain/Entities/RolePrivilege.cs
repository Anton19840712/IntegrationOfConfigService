namespace Domain.Entities
{
    public class RolePrivilege
    {
        public Guid RoleId { get; set; }
        public Role Role { get; set; }

        public Guid PrivilegeId { get; set; }
        public Privilege Privilege { get; set; }
    }
}
