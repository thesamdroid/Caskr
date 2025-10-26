using System.ComponentModel.DataAnnotations;

namespace Caskr.server.Models;

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

public class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}
