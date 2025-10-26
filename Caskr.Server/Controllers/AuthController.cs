using Caskr.server.Models;
using Caskr.server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Caskr.server.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Register a new user with a company
        /// </summary>
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<ActionResult<RegistrationResponse>> Register([FromBody] RegistrationRequest request)
        {
            try
            {
                // Validate request
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { message = "Invalid registration data", errors = ModelState });
                }

                // Register user with Keycloak and create in local database
                var result = await _authService.RegisterUserAsync(request);

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Registration validation failed");
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Registration failed due to conflict");
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Registration failed unexpectedly");
                return StatusCode(500, new { message = "Registration failed. Please try again later." });
            }
        }

        /// <summary>
        /// Authenticate user and return JWT token
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { message = "Invalid login credentials" });
                }

                var result = await _authService.LoginAsync(request.Email, request.Password);

                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Login failed for user: {Email}", request.Email);
                return Unauthorized(new { message = "Invalid email or password" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failed unexpectedly");
                return StatusCode(500, new { message = "Login failed. Please try again later." });
            }
        }

        /// <summary>
        /// Refresh authentication token
        /// </summary>
        [HttpPost("refresh")]
        public async Task<ActionResult<LoginResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            try
            {
                var result = await _authService.RefreshTokenAsync(request.RefreshToken);
                return Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new { message = "Invalid refresh token" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token refresh failed");
                return StatusCode(500, new { message = "Token refresh failed" });
            }
        }

        /// <summary>
        /// Logout user and revoke tokens
        /// </summary>
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var userId = User.FindFirst("sub")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return BadRequest(new { message = "Invalid user session" });
                }

                await _authService.LogoutAsync(userId);
                return Ok(new { message = "Logged out successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Logout failed");
                return StatusCode(500, new { message = "Logout failed" });
            }
        }
    }

    #region Request/Response Models

    public class RegistrationRequest
    {
        [Required(ErrorMessage = "Name is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$", 
            ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, and one number")]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Company name for new company creation
        /// </summary>
        public string? CompanyName { get; set; }

        /// <summary>
        /// Existing company ID to join
        /// </summary>
        public int? CompanyId { get; set; }

        /// <summary>
        /// User type/role ID
        /// </summary>
        public int UserTypeId { get; set; } = 1;
    }

    public class RegistrationResponse
    {
        public int UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string Message { get; set; } = "Registration successful";
    }

    public class LoginRequest
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public UserInfo User { get; set; } = new();
    }

    public class UserInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public int UserTypeId { get; set; }
    }

    public class RefreshTokenRequest
    {
        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }

    #endregion
}
