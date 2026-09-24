using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace SafeBeauty.API.Authentication;

public static class ClaimsPrincipalExtensions
{
    public static string GetRequiredUserId(this ClaimsPrincipal user) =>
        user.FindFirstValue(JwtRegisteredClaimNames.Sub)
        ?? throw new UnauthorizedAccessException("The authenticated token has no subject claim.");
}
