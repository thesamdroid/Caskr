using Caskr.server.Models;
using Caskr.server.Models.Portal;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Caskr.server.Services.Portal;

public interface IPortalAuthService
{
    Task<PortalRegistrationResponse> RegisterAsync(PortalRegistrationRequest request);
    Task<PortalVerifyEmailResponse> VerifyEmailAsync(string token);
    Task<PortalLoginResponse> LoginAsync(PortalLoginRequest request, string? ipAddress, string? userAgent);
    Task<PortalForgotPasswordResponse> ForgotPasswordAsync(PortalForgotPasswordRequest request);
    Task<PortalResetPasswordResponse> ResetPasswordAsync(PortalResetPasswordRequest request);
    Task LogAccessAsync(long portalUserId, PortalAction action, string? resourceType = null, long? resourceId = null, string? ipAddress = null, string? userAgent = null);
}

public class PortalAuthService : IPortalAuthService
{
    private readonly CaskrDbContext _dbContext;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PortalAuthService> _logger;

    private const int MaxFailedAttempts = 5;
    private const int LockoutMinutes = 30;
    private const int TokenExpirationHours = 24;
    private const int PasswordResetTokenExpirationHours = 1;

    public PortalAuthService(
        CaskrDbContext dbContext,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<PortalAuthService> logger)
    {
        _dbContext = dbContext;
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<PortalRegistrationResponse> RegisterAsync(PortalRegistrationRequest request)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        // Check if email already exists
        var existingUser = await _dbContext.PortalUsers
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail);

        if (existingUser != null)
        {
            throw new InvalidOperationException("An account with this email already exists.");
        }

        // Verify company exists
        var company = await _dbContext.Companies.FindAsync(request.CompanyId);
        if (company == null)
        {
            throw new ArgumentException("Invalid company ID.");
        }

        // Hash password using BCrypt
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        // Generate verification token
        var verificationToken = Guid.NewGuid().ToString();

        // Create portal user
        var portalUser = new PortalUser
        {
            Email = normalizedEmail,
            PasswordHash = passwordHash,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Phone = request.Phone?.Trim(),
            CompanyId = request.CompanyId,
            IsActive = true,
            EmailVerified = false,
            VerificationToken = verificationToken,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.PortalUsers.Add(portalUser);
        await _dbContext.SaveChangesAsync();

        // Send verification email
        var portalBaseUrl = _configuration["Portal:BaseUrl"] ?? "https://portal.caskr.com";
        var verificationLink = $"{portalBaseUrl}/verify?token={verificationToken}";

        await _emailService.SendEmailAsync(
            normalizedEmail,
            "Verify your Caskr Portal account",
            $"Welcome to Caskr Portal!\n\nPlease verify your email by clicking the link below:\n\n{verificationLink}\n\nIf you did not create this account, please ignore this email.");

        _logger.LogInformation("Portal user registered: {Email} (ID: {UserId})", normalizedEmail, portalUser.Id);

        return new PortalRegistrationResponse
        {
            UserId = portalUser.Id,
            Email = normalizedEmail,
            Message = "Registration successful. Please check your email to verify your account."
        };
    }

    public async Task<PortalVerifyEmailResponse> VerifyEmailAsync(string token)
    {
        var portalUser = await _dbContext.PortalUsers
            .FirstOrDefaultAsync(u => u.VerificationToken == token);

        if (portalUser == null)
        {
            return new PortalVerifyEmailResponse
            {
                Success = false,
                Message = "Invalid or expired verification token."
            };
        }

        if (portalUser.EmailVerified)
        {
            return new PortalVerifyEmailResponse
            {
                Success = true,
                Message = "Email already verified. You can log in."
            };
        }

        portalUser.EmailVerified = true;
        portalUser.VerificationToken = null;
        portalUser.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Portal user email verified: {Email} (ID: {UserId})", portalUser.Email, portalUser.Id);

        return new PortalVerifyEmailResponse
        {
            Success = true,
            Message = "Email verified successfully. You can now log in."
        };
    }

    public async Task<PortalLoginResponse> LoginAsync(PortalLoginRequest request, string? ipAddress, string? userAgent)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var portalUser = await _dbContext.PortalUsers
            .Include(u => u.Company)
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail);

        if (portalUser == null)
        {
            _logger.LogWarning("Portal login failed: email not found - {Email}", normalizedEmail);
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        // Check if account is locked
        if (portalUser.LockoutUntil.HasValue && portalUser.LockoutUntil > DateTime.UtcNow)
        {
            var minutesRemaining = (int)(portalUser.LockoutUntil.Value - DateTime.UtcNow).TotalMinutes + 1;
            await LogAccessAsync(portalUser.Id, PortalAction.Login_Failed, ipAddress: ipAddress, userAgent: userAgent);
            throw new UnauthorizedAccessException($"Account is locked. Please try again in {minutesRemaining} minutes.");
        }

        // Check if account is active
        if (!portalUser.IsActive)
        {
            await LogAccessAsync(portalUser.Id, PortalAction.Login_Failed, ipAddress: ipAddress, userAgent: userAgent);
            throw new UnauthorizedAccessException("Account is inactive. Please contact support.");
        }

        // Check if email is verified
        if (!portalUser.EmailVerified)
        {
            await LogAccessAsync(portalUser.Id, PortalAction.Login_Failed, ipAddress: ipAddress, userAgent: userAgent);
            throw new UnauthorizedAccessException("Please verify your email before logging in.");
        }

        // Verify password
        if (!BCrypt.Net.BCrypt.Verify(request.Password, portalUser.PasswordHash))
        {
            portalUser.FailedLoginAttempts++;

            // Lock account if too many failed attempts
            if (portalUser.FailedLoginAttempts >= MaxFailedAttempts)
            {
                portalUser.LockoutUntil = DateTime.UtcNow.AddMinutes(LockoutMinutes);
                _logger.LogWarning("Portal account locked due to too many failed attempts: {Email}", normalizedEmail);
            }

            await _dbContext.SaveChangesAsync();
            await LogAccessAsync(portalUser.Id, PortalAction.Login_Failed, ipAddress: ipAddress, userAgent: userAgent);

            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        // Successful login - reset failed attempts
        portalUser.FailedLoginAttempts = 0;
        portalUser.LockoutUntil = null;
        portalUser.LastLoginAt = DateTime.UtcNow;
        portalUser.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        // Log successful login
        await LogAccessAsync(portalUser.Id, PortalAction.Login, ipAddress: ipAddress, userAgent: userAgent);

        // Generate JWT token
        var token = GenerateJwtToken(portalUser);
        var expiresAt = DateTime.UtcNow.AddHours(TokenExpirationHours);

        _logger.LogInformation("Portal user logged in: {Email} (ID: {UserId})", normalizedEmail, portalUser.Id);

        return new PortalLoginResponse
        {
            AccessToken = token,
            ExpiresAt = expiresAt,
            User = new PortalUserInfo
            {
                Id = portalUser.Id,
                Email = portalUser.Email,
                FirstName = portalUser.FirstName,
                LastName = portalUser.LastName,
                CompanyId = portalUser.CompanyId,
                CompanyName = portalUser.Company?.CompanyName ?? "Unknown"
            }
        };
    }

    public async Task<PortalForgotPasswordResponse> ForgotPasswordAsync(PortalForgotPasswordRequest request)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var portalUser = await _dbContext.PortalUsers
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail);

        // Always return success message to prevent email enumeration
        if (portalUser == null)
        {
            _logger.LogWarning("Portal password reset requested for non-existent email: {Email}", normalizedEmail);
            return new PortalForgotPasswordResponse();
        }

        // Generate password reset token
        var resetToken = Guid.NewGuid().ToString();
        portalUser.PasswordResetToken = resetToken;
        portalUser.PasswordResetTokenExpiresAt = DateTime.UtcNow.AddHours(PasswordResetTokenExpirationHours);
        portalUser.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        // Send password reset email
        var portalBaseUrl = _configuration["Portal:BaseUrl"] ?? "https://portal.caskr.com";
        var resetLink = $"{portalBaseUrl}/reset-password?token={resetToken}";

        await _emailService.SendEmailAsync(
            normalizedEmail,
            "Reset your Caskr Portal password",
            $"You requested to reset your Caskr Portal password.\n\nClick the link below to reset your password:\n\n{resetLink}\n\nThis link will expire in {PasswordResetTokenExpirationHours} hour(s).\n\nIf you did not request this, please ignore this email.");

        _logger.LogInformation("Portal password reset requested for: {Email}", normalizedEmail);

        return new PortalForgotPasswordResponse();
    }

    public async Task<PortalResetPasswordResponse> ResetPasswordAsync(PortalResetPasswordRequest request)
    {
        var portalUser = await _dbContext.PortalUsers
            .FirstOrDefaultAsync(u => u.PasswordResetToken == request.Token);

        if (portalUser == null)
        {
            throw new InvalidOperationException("Invalid or expired password reset token.");
        }

        if (portalUser.PasswordResetTokenExpiresAt.HasValue &&
            portalUser.PasswordResetTokenExpiresAt < DateTime.UtcNow)
        {
            throw new InvalidOperationException("Password reset token has expired. Please request a new one.");
        }

        // Hash new password
        portalUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        portalUser.PasswordResetToken = null;
        portalUser.PasswordResetTokenExpiresAt = null;
        portalUser.FailedLoginAttempts = 0;
        portalUser.LockoutUntil = null;
        portalUser.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Portal password reset completed for: {Email} (ID: {UserId})", portalUser.Email, portalUser.Id);

        return new PortalResetPasswordResponse();
    }

    public async Task LogAccessAsync(long portalUserId, PortalAction action, string? resourceType = null, long? resourceId = null, string? ipAddress = null, string? userAgent = null)
    {
        var accessLog = new PortalAccessLog
        {
            PortalUserId = portalUserId,
            Action = action,
            ResourceType = resourceType,
            ResourceId = resourceId,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            AccessedAt = DateTime.UtcNow
        };

        _dbContext.PortalAccessLogs.Add(accessLog);
        await _dbContext.SaveChangesAsync();
    }

    private string GenerateJwtToken(PortalUser portalUser)
    {
        var rawSigningKey = _configuration["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(rawSigningKey))
        {
            throw new InvalidOperationException("JWT signing key is not configured.");
        }

        var signingKeyBytes = Encoding.UTF8.GetBytes(rawSigningKey);
        if (signingKeyBytes.Length < 32)
        {
            signingKeyBytes = SHA256.HashData(signingKeyBytes);
        }

        var key = new SymmetricSecurityKey(signingKeyBytes);
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, portalUser.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, portalUser.Email),
            new Claim("portalUserId", portalUser.Id.ToString()),
            new Claim("companyId", portalUser.CompanyId.ToString()),
            new Claim("role", "PortalUser"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(TokenExpirationHours),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
