namespace AuthSmith.Contracts.Roles;

public class AssignPermissionRequestDto
{
    public List<Guid> PermissionIds { get; set; } = [];
}

