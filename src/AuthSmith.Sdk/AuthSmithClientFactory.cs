using AuthSmith.Sdk.Applications;
using AuthSmith.Sdk.Auth;
using AuthSmith.Sdk.Authorization;
using AuthSmith.Sdk.Permissions;
using AuthSmith.Sdk.Roles;
using AuthSmith.Sdk.Users;
using Refit;

namespace AuthSmith.Sdk;

/// <summary>
/// Factory for creating AuthSmith API clients with automatic API key injection and retry policies.
/// </summary>
public static class AuthSmithClientFactory
{
    /// <summary>
    /// Creates an HTTP client configured for AuthSmith API with API key authentication and retry policies.
    /// </summary>
    public static HttpClient CreateHttpClient(string baseAddress, string apiKey)
    {
        var httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseAddress)
        };

        // Add API key to default headers
        httpClient.DefaultRequestHeaders.Add("X-API-Key", apiKey);

        return httpClient;
    }

    /// <summary>
    /// Creates an auth client with retry policies.
    /// </summary>
    public static IAuthClient CreateAuthClient(HttpClient httpClient)
    {
        return RestService.For<IAuthClient>(httpClient);
    }

    /// <summary>
    /// Creates an authorization client with retry policies.
    /// </summary>
    public static IAuthorizationClient CreateAuthorizationClient(HttpClient httpClient)
    {
        return RestService.For<IAuthorizationClient>(httpClient);
    }

    /// <summary>
    /// Creates a users client with retry policies.
    /// </summary>
    public static IUsersClient CreateUsersClient(HttpClient httpClient)
    {
        return RestService.For<IUsersClient>(httpClient);
    }

    /// <summary>
    /// Creates an applications client with retry policies.
    /// </summary>
    public static IApplicationsClient CreateApplicationsClient(HttpClient httpClient)
    {
        return RestService.For<IApplicationsClient>(httpClient);
    }

    /// <summary>
    /// Creates a roles client with retry policies.
    /// </summary>
    public static IRolesClient CreateRolesClient(HttpClient httpClient)
    {
        return RestService.For<IRolesClient>(httpClient);
    }

    /// <summary>
    /// Creates a permissions client with retry policies.
    /// </summary>
    public static IPermissionsClient CreatePermissionsClient(HttpClient httpClient)
    {
        return RestService.For<IPermissionsClient>(httpClient);
    }

}

