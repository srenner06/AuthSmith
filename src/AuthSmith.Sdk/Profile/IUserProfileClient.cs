using AuthSmith.Contracts.Users;
using Refit;

namespace AuthSmith.Sdk.Profile;

/// <summary>
/// Client for user profile endpoints.
/// </summary>
public interface IUserProfileClient
{
    /// <summary>
    /// Get the authenticated user's profile.
    /// </summary>
    [Get("/api/v1/profile")]
    Task<UserProfileDto> GetProfileAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Update the authenticated user's profile.
    /// </summary>
    [Patch("/api/v1/profile")]
    Task<UserProfileDto> UpdateProfileAsync(
        [Body] UpdateProfileDto request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Change the authenticated user's password.
    /// </summary>
    [Post("/api/v1/profile/change-password")]
    Task ChangePasswordAsync(
        [Body] ChangePasswordDto request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete the authenticated user's account.
    /// </summary>
    [Delete("/api/v1/profile")]
    Task DeleteAccountAsync(
        [Body] DeleteAccountDto request,
        CancellationToken cancellationToken = default);
}
