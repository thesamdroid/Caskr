using Caskr.server.Models.Portal;
using Caskr.server.Services.Portal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Caskr.server.Controllers;

/// <summary>
/// Authentication controller for customer portal.
/// Separate from main app authentication (customers don't need Keycloak SSO).
/// </summary>
[ApiController]
[Route("api/portal/auth")]
public class PortalAuthController : ControllerBase
{
    private readonly IPortalAuthService _portalAuthService;
    private readonly ILogger<PortalAuthController> _logger;

    public PortalAuthController(IPortalAuthService portalAuthService, ILogger<PortalAuthController> logger)
    {
        _portalAuthService = portalAuthService;
        _logger = logger;
    }

    /// <summary>
    /// Register a new portal user
    /// </summary>
    /// <remarks>
    /// Creates a new customer portal account. The user must verify their email before logging in.
    /// </remarks>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PortalRegistrationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PortalRegistrationResponse>> Register([FromBody] PortalRegistrationRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Invalid registration data", errors = ModelState });
            }

            var result = await _portalAuthService.RegisterAsync(request);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Portal registration validation failed for {Email}", request.Email);
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Portal registration conflict for {Email}", request.Email);
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Portal registration failed unexpectedly for {Email}", request.Email);
            return StatusCode(500, new { message = "Registration failed. Please try again later." });
        }
    }

    /// <summary>
    /// Verify email address using verification token
    /// </summary>
    /// <remarks>
    /// This endpoint is called when the user clicks the verification link in their email.
    /// </remarks>
    [HttpGet("verify")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PortalVerifyEmailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PortalVerifyEmailResponse>> VerifyEmail([FromQuery] string token)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return BadRequest(new { message = "Verification token is required." });
            }

            var result = await _portalAuthService.VerifyEmailAsync(token);

            if (!result.Success)
            {
                return BadRequest(new { message = result.Message });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email verification failed for token");
            return StatusCode(500, new { message = "Verification failed. Please try again later." });
        }
    }

    /// <summary>
    /// Authenticate portal user and return JWT token
    /// </summary>
    /// <remarks>
    /// Returns a JWT access token valid for 24 hours.
    /// Account will be locked for 30 minutes after 5 failed login attempts.
    /// </remarks>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PortalLoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PortalLoginResponse>> Login([FromBody] PortalLoginRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Invalid login credentials" });
            }

            var ipAddress = GetClientIpAddress();
            var userAgent = Request.Headers.UserAgent.ToString();

            var result = await _portalAuthService.LoginAsync(request, ipAddress, userAgent);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Portal login failed for {Email}: {Message}", request.Email, ex.Message);
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Portal login failed unexpectedly for {Email}", request.Email);
            return StatusCode(500, new { message = "Login failed. Please try again later." });
        }
    }

    /// <summary>
    /// Request password reset email
    /// </summary>
    /// <remarks>
    /// Sends a password reset link to the specified email if the account exists.
    /// For security, always returns success even if the email doesn't exist.
    /// </remarks>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PortalForgotPasswordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PortalForgotPasswordResponse>> ForgotPassword([FromBody] PortalForgotPasswordRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Invalid email format" });
            }

            var result = await _portalAuthService.ForgotPasswordAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Portal forgot password failed for {Email}", request.Email);
            // Still return success to prevent email enumeration
            return Ok(new PortalForgotPasswordResponse());
        }
    }

    /// <summary>
    /// Reset password using reset token
    /// </summary>
    /// <remarks>
    /// Resets the password using the token from the password reset email.
    /// Token expires after 1 hour.
    /// </remarks>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PortalResetPasswordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PortalResetPasswordResponse>> ResetPassword([FromBody] PortalResetPasswordRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Invalid request data", errors = ModelState });
            }

            var result = await _portalAuthService.ResetPasswordAsync(request);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Portal password reset failed");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Portal password reset failed unexpectedly");
            return StatusCode(500, new { message = "Password reset failed. Please try again later." });
        }
    }

    /// <summary>
    /// Logout portal user (client-side token removal)
    /// </summary>
    /// <remarks>
    /// This endpoint logs the logout action for audit purposes.
    /// The actual token invalidation happens client-side.
    /// </remarks>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout()
    {
        try
        {
            var portalUserId = User.FindFirst("portalUserId")?.Value;
            if (!string.IsNullOrEmpty(portalUserId) && long.TryParse(portalUserId, out var userId))
            {
                var ipAddress = GetClientIpAddress();
                var userAgent = Request.Headers.UserAgent.ToString();

                await _portalAuthService.LogAccessAsync(userId, PortalAction.Logout, ipAddress: ipAddress, userAgent: userAgent);
            }

            return Ok(new { message = "Logged out successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Portal logout failed");
            return Ok(new { message = "Logged out successfully" }); // Still return success
        }
    }

    private string? GetClientIpAddress()
    {
        // Check for forwarded IP (when behind proxy/load balancer)
        var forwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',').FirstOrDefault()?.Trim();
        }

        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }
}
