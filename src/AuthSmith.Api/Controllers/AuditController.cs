using Asp.Versioning;
using AuthSmith.Api.Authorization;
using AuthSmith.Application.Services.Audit;
using AuthSmith.Contracts.Audit;
using Microsoft.AspNetCore.Mvc;

namespace AuthSmith.Api.Controllers;

[ApiController]
[Route("api/v1/audit")]
[ApiVersion("1.0")]
public class AuditController : ControllerBase
{
    private readonly IAuditQueryService _auditQueryService;

    public AuditController(IAuditQueryService auditQueryService)
    {
        _auditQueryService = auditQueryService;
    }

    /// <summary>
    /// Query audit logs (admin only).
    /// </summary>
    /// <remarks>
    /// Returns paginated audit logs with optional filtering by user, application, event type, and date range.
    /// </remarks>
    [HttpGet]
    [RequireAdminAccess]
    [ProducesResponseType(typeof(AuditLogPageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AuditLogPageDto>> QueryLogsAsync(
        [FromQuery] AuditLogQueryDto query,
        CancellationToken cancellationToken)
    {
        var result = await _auditQueryService.QueryLogsAsync(query, cancellationToken);
        return Ok(result);
    }
}
