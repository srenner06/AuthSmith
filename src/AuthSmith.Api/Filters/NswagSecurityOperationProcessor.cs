using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;

namespace AuthSmith.Api.Filters;

/// <summary>
/// NSwag operation processor that applies API key security to all endpoints.
/// For AllowAnonymous endpoints, the lock icon will be open (optional).
/// For protected endpoints, the lock icon will be closed (required).
/// </summary>
public class NswagSecurityOperationProcessor : IOperationProcessor
{
    public bool Process(OperationProcessorContext context)
    {
        var operation = context.OperationDescription.Operation;

        // Always add API key security requirement for all endpoints
        // This allows Swagger UI to send the API key header if it's set globally
        operation.Security ??= [];
        operation.Security.Clear();

        operation.Security.Add(new NSwag.OpenApiSecurityRequirement
        {
            { "ApiKey", Array.Empty<string>() }
        });

        return true;
    }
}

