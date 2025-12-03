using System.Net;
using System.Net.Http;
using System.Text;
using Caskr.server.Models;
using Caskr.server.Services;

namespace Caskr.Server.Tests;

public class KeycloakClientTests
{
    private readonly Dictionary<string, string?> _configValues = new()
    {
        ["Keycloak:Authority"] = "http://keycloak.local",
        ["Keycloak:Realm"] = "test-realm",
        ["Keycloak:ClientId"] = "client-id",
        ["Keycloak:ClientSecret"] = "client-secret",
        ["Keycloak:AdminUser"] = "admin",
        ["Keycloak:AdminPassword"] = "admin-pass"
    };

    [Fact]
    public async Task GetTokenAsync_ReturnsToken_WhenRequestSucceeds()
    {
        var handler = new SequenceHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"access_token\":\"token-123\"}", Encoding.UTF8, "application/json")
        });

        var client = CreateClient(handler);

        var token = await client.GetTokenAsync("user@example.com", "password");

        Assert.Equal("token-123", token);
        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Contains("/protocol/openid-connect/token", request.RequestUri!.AbsoluteUri);

        var body = await request.Content!.ReadAsStringAsync();
        Assert.Contains("username=user%40example.com", body);
        Assert.Contains("grant_type=password", body);
    }

    [Fact]
    public async Task GetTokenAsync_ReturnsNull_WhenTokenMissing()
    {
        var handler = new SequenceHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        });

        var client = CreateClient(handler);

        var token = await client.GetTokenAsync("user@example.com", "password");

        Assert.Null(token);
    }

    [Fact]
    public async Task CreateUserAsync_DoesNotSendRequest_WhenAdminTokenUnavailable()
    {
        var handler = new SequenceHandler(new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var client = CreateClient(handler);

        await client.CreateUserAsync(new User { Email = "new.user@example.com" }, "temp123");

        var request = Assert.Single(handler.Requests);
        Assert.Contains("/protocol/openid-connect/token", request.RequestUri!.AbsoluteUri);
    }

    [Fact]
    public async Task CreateUserAsync_SendsUserRequest_WhenAdminTokenRetrieved()
    {
        var handler = new SequenceHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"access_token\":\"admin-token\"}", Encoding.UTF8, "application/json")
            },
            new HttpResponseMessage(HttpStatusCode.Created));

        var client = CreateClient(handler);

        await client.CreateUserAsync(new User { Email = "new.user@example.com" }, "Temp!234");

        Assert.Equal(2, handler.Requests.Count);
        var createRequest = handler.Requests[1];

        Assert.Equal(HttpMethod.Post, createRequest.Method);
        Assert.Contains("/admin/realms/test-realm/users", createRequest.RequestUri!.AbsoluteUri);
        Assert.Equal("Bearer", createRequest.Headers.Authorization!.Scheme);
        Assert.Equal("admin-token", createRequest.Headers.Authorization.Parameter);

        var payload = await createRequest.Content!.ReadAsStringAsync();
        Assert.Contains("new.user@example.com", payload);
        Assert.Contains("Temp!234", payload);
    }

    private KeycloakClient CreateClient(SequenceHandler handler)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(_configValues!)
            .Build();

        return new KeycloakClient(new HttpClient(handler), configuration);
    }

    private class SequenceHandler : DelegatingHandler
    {
        private readonly Queue<HttpResponseMessage> _responses;

        public SequenceHandler(params HttpResponseMessage[] responses)
        {
            _responses = new Queue<HttpResponseMessage>(responses);
            InnerHandler = new HttpClientHandler();
        }

        public List<HttpRequestMessage> Requests { get; } = new();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);

            if (_responses.Count == 0)
            {
                throw new InvalidOperationException("No configured response for request.");
            }

            return Task.FromResult(_responses.Dequeue());
        }
    }
}
