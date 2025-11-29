using Caskr.server.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Caskr.server.Services
{
    public interface IAuthService
    {
        Task<RegistrationResponse> RegisterUserAsync(RegistrationRequest request);
        Task<LoginResponse> LoginAsync(string email, string password);
        Task<LoginResponse> RefreshTokenAsync(string refreshToken);
        Task LogoutAsync(string userId);
    }

    public class AuthService : IAuthService
    {
        private readonly CaskrDbContext _dbContext;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;

        // Keycloak configuration
        private readonly string _keycloakBaseUrl;
        private readonly string _keycloakRealm;
        private readonly string _keycloakClientId;
        private readonly string _keycloakClientSecret;

        public AuthService(
            CaskrDbContext dbContext,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<AuthService> logger)
        {
            _dbContext = dbContext;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;

            // Load Keycloak configuration
            _keycloakBaseUrl = configuration["Keycloak:BaseUrl"] ?? "http://localhost:8080";
            _keycloakRealm = configuration["Keycloak:Realm"] ?? "caskr";
            _keycloakClientId = configuration["Keycloak:ClientId"] ?? "caskr-client";
            _keycloakClientSecret = configuration["Keycloak:ClientSecret"] ?? "";
        }

        public async Task<RegistrationResponse> RegisterUserAsync(RegistrationRequest request)
        {
            // Validate request
            if (string.IsNullOrWhiteSpace(request.CompanyName) && !request.CompanyId.HasValue)
            {
                throw new ArgumentException("Either CompanyName or CompanyId must be provided");
            }

            // Start transaction for data consistency
            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                // Check if user already exists
                var existingUser = await _dbContext.Users
                    .FirstOrDefaultAsync(u => u.Email == request.Email);

                if (existingUser != null)
                {
                    throw new InvalidOperationException("A user with this email already exists");
                }

                // Handle company creation or selection
                int companyId;
                string companyName;

                if (request.CompanyId.HasValue)
                {
                    // Join existing company
                    var company = await _dbContext.Companies.FindAsync(request.CompanyId.Value);
                    if (company == null)
                    {
                        throw new ArgumentException("Company not found");
                    }
                    companyId = company.Id;
                    companyName = company.CompanyName ?? "Unknown Company";
                }
                else
                {
                    // Create new company
                    var newCompany = new Company
                    {
                        CompanyName = request.CompanyName!.Trim(),
                        CreatedAt = DateTime.UtcNow
                    };

                    _dbContext.Companies.Add(newCompany);
                    await _dbContext.SaveChangesAsync();

                    companyId = newCompany.Id;
                    companyName = newCompany.CompanyName ?? "Unknown Company";

                    _logger.LogInformation("Created new company: {CompanyName} (ID: {CompanyId})", 
                        companyName, companyId);
                }

                // Create user in Keycloak
                var keycloakUserId = await CreateKeycloakUserAsync(request.Email, request.Name, request.Password);

                // Create user in local database
                var user = new User
                {
                    Name = request.Name.Trim(),
                    Email = request.Email.Trim().ToLowerInvariant(),
                    UserTypeId = request.UserTypeId,
                    CompanyId = companyId,
                    KeycloakUserId = keycloakUserId,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _dbContext.Users.Add(user);
                await _dbContext.SaveChangesAsync();

                // Commit transaction
                await transaction.CommitAsync();

                _logger.LogInformation("User registered successfully: {Email} (ID: {UserId}, Company: {CompanyId})",
                    user.Email, user.Id, companyId);

                return new RegistrationResponse
                {
                    UserId = user.Id,
                    Email = user.Email,
                    Name = user.Name,
                    CompanyId = companyId,
                    CompanyName = companyName
                };
            }
            catch (Exception ex)
            {
                // Rollback transaction on error
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Registration failed for email: {Email}", request.Email);
                throw;
            }
        }

        public async Task<LoginResponse> LoginAsync(string email, string password)
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();

            // Authenticate with Keycloak
            var keycloakTokens = await AuthenticateWithKeycloakAsync(normalizedEmail, password);

            // Get user from database
            var users = await _dbContext.Users
                .Include(u => u.Company)
                .Include(u => u.UserType)
                .ToListAsync();

            var user = _dbContext.Users.Local.FirstOrDefault(u =>
                    string.Equals(u.Email?.Trim(), normalizedEmail, StringComparison.OrdinalIgnoreCase))
                ?? users.FirstOrDefault(u =>
                    string.Equals(u.Email?.Trim(), normalizedEmail, StringComparison.OrdinalIgnoreCase));

            if (user == null)
            {
                throw new UnauthorizedAccessException("Invalid credentials");
            }

            if (!user.IsActive)
            {
                throw new UnauthorizedAccessException("User account is inactive");
            }

            // Update last login time
            user.LastLoginAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("User logged in successfully: {Email} (ID: {UserId})",
                user.Email, user.Id);

            return new LoginResponse
            {
                Token = keycloakTokens.AccessToken,
                RefreshToken = keycloakTokens.RefreshToken,
                ExpiresAt = DateTime.UtcNow.AddSeconds(keycloakTokens.ExpiresIn),
                User = new UserInfo
                {
                    Id = user.Id,
                    Name = user.Name,
                    Email = user.Email,
                    CompanyId = user.CompanyId,
                    CompanyName = user.Company?.CompanyName ?? "Unknown",
                    UserTypeId = user.UserTypeId,
                    Role = user.UserType?.Name ?? string.Empty,
                    Permissions = BuildPermissionsFor(user)
                }
            };
        }

        public async Task<LoginResponse> RefreshTokenAsync(string refreshToken)
        {
            var keycloakTokens = await RefreshKeycloakTokenAsync(refreshToken);

            // Decode token to get user email (in production, use proper JWT parsing)
            // For now, we'll need the user to be identified another way
            // This is a simplified version - in production, parse JWT claims

            return new LoginResponse
            {
                Token = keycloakTokens.AccessToken,
                RefreshToken = keycloakTokens.RefreshToken,
                ExpiresAt = DateTime.UtcNow.AddSeconds(keycloakTokens.ExpiresIn),
                User = new UserInfo() // Populate from token claims in production
            };
        }

        public async Task LogoutAsync(string userId)
        {
            // Revoke tokens in Keycloak
            await RevokeKeycloakTokenAsync(userId);

            _logger.LogInformation("User logged out: {UserId}", userId);
        }

        #region Keycloak Integration Methods

        private static List<string> BuildPermissionsFor(User user)
        {
            var permissions = new List<string>();
            var roleName = user.UserType?.Name ?? string.Empty;

            if (user.IsPrimaryContact)
            {
                permissions.Add("TTB_COMPLIANCE");
            }

            if (roleName.Contains("admin", StringComparison.OrdinalIgnoreCase) ||
                roleName.Contains("compliance", StringComparison.OrdinalIgnoreCase) ||
                roleName.Contains("operations", StringComparison.OrdinalIgnoreCase))
            {
                permissions.Add("TTB_COMPLIANCE");
            }

            return permissions.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        private async Task<string> CreateKeycloakUserAsync(string email, string name, string password)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var adminToken = await GetKeycloakAdminTokenAsync();

                // Prepare user creation request
                var userPayload = new
                {
                    username = email,
                    email = email,
                    firstName = name.Split(' ').FirstOrDefault() ?? name,
                    lastName = name.Contains(' ') ? string.Join(" ", name.Split(' ').Skip(1)) : "",
                    enabled = true,
                    emailVerified = false,
                    credentials = new[]
                    {
                        new
                        {
                            type = "password",
                            value = password,
                            temporary = false
                        }
                    }
                };

                var url = $"{_keycloakBaseUrl}/admin/realms/{_keycloakRealm}/users";
                var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new StringContent(
                        JsonSerializer.Serialize(userPayload),
                        Encoding.UTF8,
                        "application/json")
                };

                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

                var response = await client.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Keycloak user creation failed: {StatusCode} - {Error}", 
                        response.StatusCode, error);
                    throw new InvalidOperationException("Failed to create user in Keycloak");
                }

                // Extract user ID from Location header
                var locationHeader = response.Headers.Location?.ToString();
                if (string.IsNullOrEmpty(locationHeader))
                {
                    throw new InvalidOperationException("Could not retrieve Keycloak user ID");
                }

                var keycloakUserId = locationHeader.Split('/').Last();
                return keycloakUserId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create Keycloak user for email: {Email}", email);
                throw;
            }
        }

        private async Task<KeycloakTokenResponse> AuthenticateWithKeycloakAsync(string email, string password)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var url = $"{_keycloakBaseUrl}/realms/{_keycloakRealm}/protocol/openid-connect/token";

                var requestContent = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("client_id", _keycloakClientId),
                    new KeyValuePair<string, string>("client_secret", _keycloakClientSecret),
                    new KeyValuePair<string, string>("grant_type", "password"),
                    new KeyValuePair<string, string>("username", email),
                    new KeyValuePair<string, string>("password", password)
                });

                var response = await client.PostAsync(url, requestContent);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Keycloak authentication failed: {StatusCode} - {Error}", 
                        response.StatusCode, error);
                    throw new UnauthorizedAccessException("Invalid credentials");
                }

                var content = await response.Content.ReadAsStringAsync();
                var tokenResponse = JsonSerializer.Deserialize<KeycloakTokenResponse>(content, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (tokenResponse == null)
                {
                    throw new InvalidOperationException("Invalid token response from Keycloak");
                }

                return tokenResponse;
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Keycloak authentication failed for user: {Email}", email);
                throw new InvalidOperationException("Authentication service unavailable", ex);
            }
        }

        private async Task<KeycloakTokenResponse> RefreshKeycloakTokenAsync(string refreshToken)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var url = $"{_keycloakBaseUrl}/realms/{_keycloakRealm}/protocol/openid-connect/token";

                var requestContent = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("client_id", _keycloakClientId),
                    new KeyValuePair<string, string>("client_secret", _keycloakClientSecret),
                    new KeyValuePair<string, string>("grant_type", "refresh_token"),
                    new KeyValuePair<string, string>("refresh_token", refreshToken)
                });

                var response = await client.PostAsync(url, requestContent);

                if (!response.IsSuccessStatusCode)
                {
                    throw new UnauthorizedAccessException("Invalid refresh token");
                }

                var content = await response.Content.ReadAsStringAsync();
                var tokenResponse = JsonSerializer.Deserialize<KeycloakTokenResponse>(content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (tokenResponse == null)
                {
                    throw new InvalidOperationException("Invalid token response");
                }

                return tokenResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token refresh failed");
                throw;
            }
        }

        private async Task RevokeKeycloakTokenAsync(string userId)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var adminToken = await GetKeycloakAdminTokenAsync();

                var url = $"{_keycloakBaseUrl}/admin/realms/{_keycloakRealm}/users/{userId}/logout";
                var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

                await client.SendAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to revoke Keycloak tokens for user: {UserId}", userId);
                // Don't throw - logout should succeed even if Keycloak revocation fails
            }
        }

        private async Task<string> GetKeycloakAdminTokenAsync()
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var url = $"{_keycloakBaseUrl}/realms/master/protocol/openid-connect/token";

                var adminUsername = _configuration["Keycloak:AdminUsername"] ?? "admin";
                var adminPassword = _configuration["Keycloak:AdminPassword"] ?? "admin";

                var requestContent = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("client_id", "admin-cli"),
                    new KeyValuePair<string, string>("grant_type", "password"),
                    new KeyValuePair<string, string>("username", adminUsername),
                    new KeyValuePair<string, string>("password", adminPassword)
                });

                var response = await client.PostAsync(url, requestContent);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var tokenResponse = JsonSerializer.Deserialize<KeycloakTokenResponse>(content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return tokenResponse?.AccessToken ?? throw new InvalidOperationException("No admin token received");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get Keycloak admin token");
                throw new InvalidOperationException("Could not authenticate with Keycloak admin API", ex);
            }
        }

        #endregion

        #region Helper Classes

        private class KeycloakTokenResponse
        {
            [JsonPropertyName("access_token")]
            public string AccessToken { get; set; } = string.Empty;

            [JsonPropertyName("refresh_token")]
            public string RefreshToken { get; set; } = string.Empty;

            [JsonPropertyName("expires_in")]
            public int ExpiresIn { get; set; }

            [JsonPropertyName("refresh_expires_in")]
            public int RefreshExpiresIn { get; set; }

            [JsonPropertyName("token_type")]
            public string TokenType { get; set; } = "Bearer";
        }

        #endregion
    }
}
