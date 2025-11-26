namespace AuthSmith.Contracts.Authorization;

public class BulkPermissionCheckResultDto
{
    public List<PermissionCheckResultItemDto> Results { get; set; } = [];
}

public class PermissionCheckResultItemDto
{
    public string Module { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public bool HasPermission { get; set; }
}

