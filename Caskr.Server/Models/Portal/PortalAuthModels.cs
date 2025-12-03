using System.ComponentModel.DataAnnotations;

namespace Caskr.server.Models.Portal;

#region Registration

public class PortalRegistrationRequest
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [StringLength(200, ErrorMessage = "Email must not exceed 200 characters")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$",
        ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, and one number")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "First name is required")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "First name must be between 1 and 100 characters")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Last name must be between 1 and 100 characters")]
    public string LastName { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Invalid phone format")]
    [StringLength(50, ErrorMessage = "Phone must not exceed 50 characters")]
    public string? Phone { get; set; }

    [Required(ErrorMessage = "Company ID is required")]
    public int CompanyId { get; set; }
}

public class PortalRegistrationResponse
{
    public string Message { get; set; } = "Registration successful. Please check your email to verify your account.";
    public long UserId { get; set; }
    public string Email { get; set; } = string.Empty;
}

#endregion

#region Login

public class PortalLoginRequest
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    public string Password { get; set; } = string.Empty;
}

public class PortalLoginResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public PortalUserInfo User { get; set; } = new();
}

public class PortalUserInfo
{
    public long Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public int CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
}

#endregion

#region Password Reset

public class PortalForgotPasswordRequest
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;
}

public class PortalForgotPasswordResponse
{
    public string Message { get; set; } = "If an account with that email exists, a password reset link has been sent.";
}

public class PortalResetPasswordRequest
{
    [Required(ErrorMessage = "Token is required")]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "New password is required")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$",
        ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, and one number")]
    public string NewPassword { get; set; } = string.Empty;
}

public class PortalResetPasswordResponse
{
    public string Message { get; set; } = "Password reset successful. You can now log in with your new password.";
}

#endregion

#region Email Verification

public class PortalVerifyEmailResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

#endregion
