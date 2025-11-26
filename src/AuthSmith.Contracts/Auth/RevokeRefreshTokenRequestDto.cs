namespace AuthSmith.Contracts.Auth;

public class RevokeRefreshTokenRequestDto
{
    public string RefreshToken { get; set; } = string.Empty;
}

