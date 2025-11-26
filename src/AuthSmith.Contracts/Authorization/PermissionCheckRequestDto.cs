namespace AuthSmith.Contracts.Authorization;

public class PermissionCheckRequestDto
{
    public Guid UserId { get; set; }
    public string ApplicationKey { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
}

