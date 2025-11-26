namespace AuthSmith.Contracts.Permissions;

public class CreatePermissionRequestDto
{
    public string Module { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? Description { get; set; }
}

