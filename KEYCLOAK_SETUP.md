# Keycloak Integration Setup Guide

## Overview
This guide explains how to integrate Keycloak authentication with the Caskr application for robust user registration and authentication with company management.

## Prerequisites
- Keycloak server running (version 20+ recommended)
- .NET 8 SDK
- PostgreSQL or SQL Server database
- Access to Keycloak admin console

---

## 1. Keycloak Configuration

### Step 1: Create Realm
1. Log into Keycloak admin console (http://localhost:8080/admin)
2. Create a new realm called `caskr`
3. Enable user registration if desired

### Step 2: Create Client
1. Navigate to Clients → Create
2. Configure the client:
   - Client ID: `caskr-client`
   - Client Protocol: `openid-connect`
   - Access Type: `confidential`
   - Valid Redirect URIs: `http://localhost:5173/*` (adjust for your frontend)
   - Web Origins: `http://localhost:5173` (adjust for your frontend)
3. Save and note the **Client Secret** from the Credentials tab

### Step 3: Configure Client Settings
1. Enable the following:
   - Standard Flow Enabled: ON
   - Direct Access Grants Enabled: ON
   - Service Accounts Enabled: ON
2. Set Access Token Lifespan: 15 minutes (or as needed)
3. Set SSO Session Idle: 30 minutes
4. Set SSO Session Max: 10 hours

---

## 2. Backend Configuration

### Step 1: Update appsettings.json
Add Keycloak configuration to your `appsettings.json`:

```json
{
  "Keycloak": {
    "BaseUrl": "http://localhost:8080",
    "Realm": "caskr",
    "ClientId": "caskr-client",
    "ClientSecret": "YOUR_CLIENT_SECRET_HERE",
    "AdminUsername": "admin",
    "AdminPassword": "admin"
  },
  "ConnectionStrings": {
    "DefaultConnection": "YOUR_CONNECTION_STRING"
  }
}
```

### Step 2: Register Services in Program.cs
Add the following to your `Program.cs`:

```csharp
using Caskr.server.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add HttpClient for Keycloak API calls
builder.Services.AddHttpClient();

// Register AuthService
builder.Services.AddScoped<IAuthService, AuthService>();

// Configure JWT Authentication
var keycloakBaseUrl = builder.Configuration["Keycloak:BaseUrl"];
var keycloakRealm = builder.Configuration["Keycloak:Realm"];

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = $"{keycloakBaseUrl}/realms/{keycloakRealm}";
        options.Audience = builder.Configuration["Keycloak:ClientId"];
        options.RequireHttpsMetadata = false; // Set to true in production
        
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// ... rest of your service configuration

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// ... rest of your middleware

app.Run();
```

### Step 3: Database Migration
Run the following commands to update your database:

```bash
# Create migration
dotnet ef migrations add AddKeycloakIntegration

# Update database
dotnet ef database update
```

---

## 3. Frontend Configuration

### Step 1: Install the Sign Up Component
Copy `SignUp.jsx` to your React components directory:
```
src/components/SignUp.jsx
```

### Step 2: Add Route
Update your routing in `App.tsx`:

```tsx
import SignUp from './components/SignUp'

function App() {
  return (
    <Routes>
      <Route path='/signup' element={<SignUp />} />
      <Route path='/login' element={<Login />} />
      {/* ... other routes */}
    </Routes>
  )
}
```

### Step 3: Update Login Component
Replace the old login component with `Login.jsx` provided.

---

## 4. API Endpoints

### Registration
**POST** `/api/auth/register`

Request body:
```json
{
  "name": "John Doe",
  "email": "john@example.com",
  "password": "SecurePass123!",
  "companyName": "Distillery Inc",
  "userTypeId": 1
}
```

Response:
```json
{
  "userId": 1,
  "email": "john@example.com",
  "name": "John Doe",
  "companyId": 1,
  "companyName": "Distillery Inc",
  "message": "Registration successful"
}
```

### Login
**POST** `/api/auth/login`

Request body:
```json
{
  "email": "john@example.com",
  "password": "SecurePass123!"
}
```

Response:
```json
{
  "token": "eyJhbGciOiJSUzI1NiIsInR5cCI...",
  "refreshToken": "eyJhbGciOiJIUzI1NiIsInR5cCI...",
  "expiresAt": "2024-10-26T14:30:00Z",
  "user": {
    "id": 1,
    "name": "John Doe",
    "email": "john@example.com",
    "companyId": 1,
    "companyName": "Distillery Inc",
    "userTypeId": 1
  }
}
```

### Token Refresh
**POST** `/api/auth/refresh`

Request body:
```json
{
  "refreshToken": "eyJhbGciOiJIUzI1NiIsInR5cCI..."
}
```

### Logout
**POST** `/api/auth/logout`

Requires: Authorization header with Bearer token

---

## 5. Security Best Practices

### Production Checklist
- [ ] Use HTTPS for all communication
- [ ] Store Keycloak credentials in environment variables or secure vault
- [ ] Enable CORS properly with specific origins
- [ ] Set secure cookie flags if using session cookies
- [ ] Implement rate limiting on authentication endpoints
- [ ] Enable email verification in Keycloak
- [ ] Configure proper password policies in Keycloak
- [ ] Set up monitoring and logging for authentication events
- [ ] Implement account lockout after failed login attempts
- [ ] Use refresh token rotation
- [ ] Set appropriate token expiration times

### Environment Variables (Production)
Instead of appsettings.json, use environment variables:

```bash
export KEYCLOAK__BASEURL="https://your-keycloak-server.com"
export KEYCLOAK__REALM="caskr"
export KEYCLOAK__CLIENTID="caskr-client"
export KEYCLOAK__CLIENTSECRET="your-secret"
export KEYCLOAK__ADMINUSERNAME="admin"
export KEYCLOAK__ADMINPASSWORD="admin-password"
```

---

## 6. Testing

### Manual Testing
1. **Registration**:
   - Navigate to `/signup`
   - Fill in all fields
   - Submit form
   - Verify user is created in both Keycloak and local database

2. **Login**:
   - Navigate to `/login`
   - Enter credentials
   - Verify JWT token is received
   - Verify user can access protected routes

3. **Token Refresh**:
   - Wait for token to near expiration
   - Use refresh token endpoint
   - Verify new tokens are received

### Automated Testing
Consider adding integration tests:

```csharp
[Fact]
public async Task Register_ValidRequest_ReturnsSuccess()
{
    // Arrange
    var request = new RegistrationRequest
    {
        Name = "Test User",
        Email = "test@example.com",
        Password = "Test123!",
        CompanyName = "Test Company"
    };

    // Act
    var response = await _authService.RegisterUserAsync(request);

    // Assert
    Assert.NotNull(response);
    Assert.Equal("test@example.com", response.Email);
}
```

---

## 7. Troubleshooting

### Common Issues

**Issue**: "Failed to create user in Keycloak"
- **Solution**: Check Keycloak admin credentials in configuration
- Verify Keycloak server is running and accessible
- Check network connectivity

**Issue**: "Invalid credentials" on login
- **Solution**: Verify user exists in Keycloak realm
- Check that realm name matches configuration
- Ensure client secret is correct

**Issue**: "Token validation failed"
- **Solution**: Verify JWT authority URL is correct
- Check that token hasn't expired
- Ensure client audience matches

**Issue**: Database connection errors
- **Solution**: Verify connection string is correct
- Ensure database migrations have been applied
- Check database server is running

---

## 8. Monitoring and Logging

### Recommended Logging
Enable structured logging for authentication events:

```csharp
_logger.LogInformation("User registration attempt: {Email}", email);
_logger.LogWarning("Failed login attempt: {Email}", email);
_logger.LogError(ex, "Keycloak integration error for user: {Email}", email);
```

### Metrics to Track
- Registration success/failure rate
- Login attempts per minute
- Token refresh frequency
- Average authentication response time
- Failed authentication attempts by IP

---

## 9. Additional Resources

- [Keycloak Documentation](https://www.keycloak.org/documentation)
- [Keycloak Admin REST API](https://www.keycloak.org/docs-api/latest/rest-api/)
- [ASP.NET Core JWT Authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/)
- [React Authentication Best Practices](https://react.dev/learn/sharing-state-between-components)

---

## Support

For issues or questions:
1. Check the troubleshooting section above
2. Review Keycloak server logs
3. Review application logs
4. Contact the development team
