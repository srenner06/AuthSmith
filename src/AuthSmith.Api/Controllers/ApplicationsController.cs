using Asp.Versioning;
using AuthSmith.Api.Authorization;
using AuthSmith.Api.Extensions;
using AuthSmith.Application.Services.Applications;
using AuthSmith.Contracts.Applications;
using Microsoft.AspNetCore.Mvc;

namespace AuthSmith.Api.Controllers;

[ApiController]
[Route("api/v1/apps")]
[ApiVersion("1.0")]
[RequireAdminAccess]
public class ApplicationsController : ControllerBase
{
    private readonly IApplicationService _applicationService;

    public ApplicationsController(IApplicationService applicationService)
    {
        _applicationService = applicationService;
    }

    /// <summary>
    /// Create a new application.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApplicationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApplicationDto>> CreateApplicationAsync(
        [FromBody] CreateApplicationRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _applicationService.CreateAsync(request, cancellationToken);
        if (result.TryPickT1(out var conflictError, out var application))
        {
            return Conflict(new { error = conflictError.Message });
        }
        return CreatedAtAction(nameof(GetApplicationAsync), new { id = application.Id }, application);
    }

    /// <summary>
    /// List all applications.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<ApplicationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ApplicationDto>>> ListApplicationsAsync(CancellationToken cancellationToken)
    {
        var applications = await _applicationService.ListAsync(cancellationToken);
        return Ok(applications);
    }

    /// <summary>
    /// Get an application by ID.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApplicationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApplicationDto>> GetApplicationAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _applicationService.GetByIdAsync(id, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Update an application.
    /// </summary>
    [HttpPatch("{id}")]
    [ProducesResponseType(typeof(ApplicationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApplicationDto>> UpdateApplicationAsync(
        Guid id,
        [FromBody] UpdateApplicationRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _applicationService.UpdateAsync(id, request, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Generate a new API key for an application.
    /// </summary>
    [HttpPost("{id}/api-key")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GenerateApiKeyAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _applicationService.GenerateApiKeyAsync(id, cancellationToken);
        if (result.TryPickT1(out var notFoundError, out var apiKey))
        {
            return NotFound(new { error = notFoundError.Message ?? "Resource not found." });
        }
        return Ok(new { apiKey });
    }
}
