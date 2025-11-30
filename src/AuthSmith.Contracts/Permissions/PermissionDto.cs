namespace AuthSmith.Contracts.Permissions;

public class PermissionDto
{
    public Guid Id { get; set; }
    public Guid ApplicationId { get; set; }
    public string Module { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
