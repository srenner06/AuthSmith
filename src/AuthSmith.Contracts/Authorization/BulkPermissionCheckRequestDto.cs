namespace AuthSmith.Contracts.Authorization;

public class BulkPermissionCheckRequestDto
{
    public Guid UserId { get; set; }
    public string ApplicationKey { get; set; } = string.Empty;
    public List<PermissionCheckItemDto> Checks { get; set; } = [];
}

public class PermissionCheckItemDto
{
    public string Module { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
}

