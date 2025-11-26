using Microsoft.AspNetCore.Authorization;
using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;

namespace AuthSmith.Api.Filters;

/// <summary>
/// NSwag operation processor that conditionally applies security requirements based on [AllowAnonymous] attribute.
/// </summary>
public class NswagSecurityOperationProcessor : IOperationProcessor
{
    public bool Process(OperationProcessorContext context)
    {
        var operation = context.OperationDescription.Operation;

        // Check if the endpoint has [AllowAnonymous] attribute
        var hasAllowAnonymous = false;
        if (context.MethodInfo != null)
        {
            hasAllowAnonymous = context.MethodInfo.GetCustomAttributes(true)
                .OfType<AllowAnonymousAttribute>()
                .Any() ||
                (context.MethodInfo.DeclaringType?.GetCustomAttributes(true)
                .OfType<AllowAnonymousAttribute>()
                .Any() ?? false);
        }

        // If AllowAnonymous, remove all security requirements
        if (hasAllowAnonymous)
        {
            operation.Security?.Clear();
        }
        else
        {
            // If not AllowAnonymous, ensure X-API-Key security requirement is present
            if (operation.Security == null)
            {
                operation.Security = new List<NSwag.OpenApiSecurityRequirement>();
            }

            // Clear any existing security requirements
            operation.Security.Clear();

            // Add API key security requirement
            operation.Security.Add(new NSwag.OpenApiSecurityRequirement
            {
                { "ApiKey", Array.Empty<string>() }
            });
        }

        return true;
    }
}

