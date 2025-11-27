namespace Domain.Entities
{
    public class Privilege
    {
        public Guid Id { get; set; }
        public string Name { get; set; }                          // Например, "ViewUsers", "ManageTokens"
        public ICollection<RolePrivilege> RolePrivileges { get; set; } = new List<RolePrivilege>();
    }
}
