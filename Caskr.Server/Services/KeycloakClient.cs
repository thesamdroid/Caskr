using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Caskr.server.Models;

namespace Caskr.server.Services;

public interface IKeycloakClient
{
    Task<string?> GetTokenAsync(string username, string password);
    Task CreateUserAsync(User user, string temporaryPassword);
}

public class KeycloakClient(HttpClient httpClient, IConfiguration configuration) : IKeycloakClient
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly IConfiguration _configuration = configuration;

    public async Task<string?> GetTokenAsync(string username, string password)
    {
        var content = new Dictionary<string, string>
        {
            ["client_id"] = _configuration["Keycloak:ClientId"]!,
            ["client_secret"] = _configuration["Keycloak:ClientSecret"]!,
            ["grant_type"] = "password",
            ["username"] = username,
            ["password"] = password
        };
        var response = await _httpClient.PostAsync(
            $"{_configuration["Keycloak:Authority"]}/realms/{_configuration["Keycloak:Realm"]}/protocol/openid-connect/token",
            new FormUrlEncodedContent(content));
        if (!response.IsSuccessStatusCode) return null;

        var responseText = await response.Content.ReadAsStringAsync();
        try
        {
            using var doc = JsonDocument.Parse(responseText);
            if (!doc.RootElement.TryGetProperty("access_token", out var tokenElement)) return null;

            var token = tokenElement.GetString();
            return string.IsNullOrWhiteSpace(token) ? null : token;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public async Task CreateUserAsync(User user, string temporaryPassword)
    {
        var adminToken = await GetTokenAsync(_configuration["Keycloak:AdminUser"]!, _configuration["Keycloak:AdminPassword"]!);
        if (adminToken is null) return;

        var payload = new
        {
            username = user.Email,
            email = user.Email,
            enabled = true,
            credentials = new[]
            {
                new { type = "password", value = temporaryPassword, temporary = false }
            }
        };
        var request = new HttpRequestMessage(HttpMethod.Post,
            $"{_configuration["Keycloak:Authority"]}/admin/realms/{_configuration["Keycloak:Realm"]}/users")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        await _httpClient.SendAsync(request);
    }
}
