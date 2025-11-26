namespace AuthSmith.Contracts.Roles;

public class AssignPermissionsRequestDto
{
    public List<Guid> PermissionIds { get; set; } = [];
}

