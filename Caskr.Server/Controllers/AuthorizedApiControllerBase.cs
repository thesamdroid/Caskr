using System.Security.Claims;
using Caskr.server.Models;
using Caskr.server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Caskr.server.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public abstract class AuthorizedApiControllerBase : ControllerBase
{
    /// <summary>
    /// Gets the current user from the database based on JWT claims.
    /// Supports both numeric user IDs (legacy) and Keycloak tokens (by email).
    /// </summary>
    protected async Task<User?> GetCurrentUserAsync(IUsersService usersService)
    {
        // First try numeric user ID (legacy tokens)
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(userIdValue, out var userId))
        {
            return await usersService.GetUserByIdAsync(userId);
        }

        // Fall back to email lookup for Keycloak tokens
        // Keycloak provides email in 'preferred_username' or 'email' claim
        var email = User.FindFirstValue("preferred_username")
                    ?? User.FindFirstValue(ClaimTypes.Email)
                    ?? User.FindFirstValue("email");

        if (!string.IsNullOrEmpty(email))
        {
            return await usersService.GetUserByEmailAsync(email);
        }

        return null;
    }
}
