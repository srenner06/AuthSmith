using AuthSmith.Contracts.Applications;
using Refit;

namespace AuthSmith.Sdk.Applications;

public interface IApplicationsClient
{
    [Post("/api/v1/apps")]
    Task<ApplicationDto> CreateAsync([Body] CreateApplicationRequestDto request);

    [Get("/api/v1/apps")]
    Task<List<ApplicationDto>> ListAsync();

    [Get("/api/v1/apps/{id}")]
    Task<ApplicationDto> GetByIdAsync(Guid id);

    [Patch("/api/v1/apps/{id}")]
    Task<ApplicationDto> UpdateAsync(Guid id, [Body] UpdateApplicationRequestDto request);

    [Post("/api/v1/apps/{id}/api-key")]
    Task<object> GenerateApiKeyAsync(Guid id);
}

