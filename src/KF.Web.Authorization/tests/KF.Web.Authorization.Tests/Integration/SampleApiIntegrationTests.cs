using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using KF.Web.Authorization.Sample.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace KF.Web.Authorization.Tests.Integration;

public class SampleApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public SampleApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Attribute_AdminOrSupport_RespectsRoles()
    {
        using var client = CreateClient();
        var adminToken = await GetTokenAsync(client, new[] { "Admin" });
        var supportToken = await GetTokenAsync(client, new[] { "Support" });
        var userToken = await GetTokenAsync(client, new[] { "User" });

        (await SendAuthorizedAsync(client, HttpMethod.Get, "/api/attr/admin-or-support", adminToken)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await SendAuthorizedAsync(client, HttpMethod.Get, "/api/attr/admin-or-support", supportToken)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await SendAuthorizedAsync(client, HttpMethod.Get, "/api/attr/admin-or-support", userToken)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Attribute_NotAnyOfSuspended_BlocksSuspendedRole()
    {
        using var client = CreateClient();
        var userToken = await GetTokenAsync(client, new[] { "User" });
        var suspendedToken = await GetTokenAsync(client, new[] { "Suspended" });

        (await SendAuthorizedAsync(client, HttpMethod.Get, "/api/attr/everyone-except-suspended", userToken)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await SendAuthorizedAsync(client, HttpMethod.Get, "/api/attr/everyone-except-suspended", suspendedToken)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Attribute_NotAllOfTraderAuditor_DeniesOnlyCombination()
    {
        using var client = CreateClient();
        var traderToken = await GetTokenAsync(client, new[] { "Trader" });
        var auditorToken = await GetTokenAsync(client, new[] { "Auditor" });
        var comboToken = await GetTokenAsync(client, new[] { "Trader", "Auditor" });

        (await SendAuthorizedAsync(client, HttpMethod.Get, "/api/attr/not-trader-and-auditor", traderToken)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await SendAuthorizedAsync(client, HttpMethod.Get, "/api/attr/not-trader-and-auditor", auditorToken)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await SendAuthorizedAsync(client, HttpMethod.Get, "/api/attr/not-trader-and-auditor", comboToken)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Attribute_BusinessHoursCondition_AllowsWithinWindow()
    {
        using var client = CreateClientWithClock(new StaticTimeProvider(new DateTimeOffset(2024, 1, 1, 10, 0, 0, TimeSpan.Zero)));
        var token = await GetTokenAsync(client, new[] { "User" });

        (await SendAuthorizedAsync(client, HttpMethod.Get, "/api/attr/business-hours-only", token)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Attribute_BusinessHoursCondition_BlocksOutsideWindow()
    {
        using var client = CreateClientWithClock(new StaticTimeProvider(new DateTimeOffset(2024, 1, 1, 2, 0, 0, TimeSpan.Zero)));
        var token = await GetTokenAsync(client, new[] { "User" });

        (await SendAuthorizedAsync(client, HttpMethod.Get, "/api/attr/business-hours-only", token)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Dynamic_ViewOrders_AllowsAdminOrSales()
    {
        using var client = CreateClient();
        var adminToken = await GetTokenAsync(client, new[] { "Admin" });
        var salesToken = await GetTokenAsync(client, new[] { "Sales" });
        var userToken = await GetTokenAsync(client, new[] { "User" });

        (await SendAuthorizedAsync(client, HttpMethod.Get, "/api/dyn/orders/view", adminToken)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await SendAuthorizedAsync(client, HttpMethod.Get, "/api/dyn/orders/view", salesToken)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await SendAuthorizedAsync(client, HttpMethod.Get, "/api/dyn/orders/view", userToken)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Dynamic_CreateOrder_RequiresInternalHeaderAndRoles()
    {
        using var client = CreateClient();
        var validToken = await GetTokenAsync(client, new[] { "Admin", "Sales" });
        var missingRoleToken = await GetTokenAsync(client, new[] { "Admin" });

        (await SendAuthorizedAsync(client, HttpMethod.Post, "/api/dyn/orders/create", validToken, request =>
        {
            request.Headers.Add("X-Request-Source", "Internal");
        })).StatusCode.Should().Be(HttpStatusCode.OK);

        (await SendAuthorizedAsync(client, HttpMethod.Post, "/api/dyn/orders/create", validToken)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await SendAuthorizedAsync(client, HttpMethod.Post, "/api/dyn/orders/create", missingRoleToken, request =>
        {
            request.Headers.Add("X-Request-Source", "Internal");
        })).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Dynamic_SensitiveReport_RequiresTenantMatchAndNoSuspension()
    {
        using var client = CreateClient();
        var matchingToken = await GetTokenAsync(client, new[] { "User" }, new Dictionary<string, string> { { "tenant_id", "123" } });
        var mismatchedToken = await GetTokenAsync(client, new[] { "User" }, new Dictionary<string, string> { { "tenant_id", "999" } });
        var suspendedToken = await GetTokenAsync(client, new[] { "Suspended" }, new Dictionary<string, string> { { "tenant_id", "123" } });

        (await SendAuthorizedAsync(client, HttpMethod.Get, "/api/dyn/reports/sensitive?tenantId=123", matchingToken)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await SendAuthorizedAsync(client, HttpMethod.Get, "/api/dyn/reports/sensitive?tenantId=123", mismatchedToken)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await SendAuthorizedAsync(client, HttpMethod.Get, "/api/dyn/reports/sensitive?tenantId=123", suspendedToken)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Dynamic_NoTokenWithRules_Returns403()
    {
        using var client = CreateClient();
        var response = await client.GetAsync("/api/dyn/orders/view");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private HttpClient CreateClient() => _factory.CreateClient();

    private HttpClient CreateClientWithClock(TimeProvider timeProvider)
    {
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<TimeProvider>();
                services.AddSingleton(timeProvider);
            });
        });

        return factory.CreateClient();
    }

    private static async Task<HttpResponseMessage> SendAuthorizedAsync(
        HttpClient client,
        HttpMethod method,
        string url,
        string token,
        Action<HttpRequestMessage>? configure = null)
    {
        using var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        configure?.Invoke(request);
        return await client.SendAsync(request);
    }

    private static async Task<string> GetTokenAsync(HttpClient client, string[] roles, Dictionary<string, string>? claims = null)
    {
        var response = await client.PostAsJsonAsync("/auth/token", new TokenRequest
        {
            UserName = $"user-{Guid.NewGuid():N}",
            Roles = roles,
            AdditionalClaims = claims ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        });

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<TokenResponseDto>()
                      ?? throw new InvalidOperationException("Token response missing");
        return payload.access_token ?? throw new InvalidOperationException("Token value missing");
    }

    private sealed class TokenResponseDto
    {
        public string? access_token { get; set; }
        public int expires_in { get; set; }
    }

    private sealed class StaticTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _utcNow;

        public StaticTimeProvider(DateTimeOffset utcNow)
        {
            _utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow() => _utcNow;
    }
}
