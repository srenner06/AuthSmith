namespace AuthSmith.Contracts.Authorization;

public class PermissionCheckResultDto
{
    public bool HasPermission { get; set; }
    public string Source { get; set; } = string.Empty; // "Role" or "Direct"
}

