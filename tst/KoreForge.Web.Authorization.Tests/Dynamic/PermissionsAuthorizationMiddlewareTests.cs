using FluentAssertions;
using KoreForge.Web.Authorization.Dynamic;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace KoreForge.Web.Authorization.Tests.Dynamic;

public class PermissionsAuthorizationMiddlewareTests
{
    [Fact]
    public async Task AuthorizedRequest_CallsNext()
    {
        var called = false;
        RequestDelegate next = _ =>
        {
            called = true;
            return Task.CompletedTask;
        };

        var middleware = new PermissionsAuthorizationMiddleware(
            NullLogger<PermissionsAuthorizationMiddleware>.Instance,
            new StubPermissionEvaluator(true));

        var context = new DefaultHttpContext();

        await middleware.InvokeAsync(context, next);

        called.Should().BeTrue();
        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task UnauthorizedRequest_DoesNotCallNext_Sets403()
    {
        var called = false;
        RequestDelegate next = _ =>
        {
            called = true;
            return Task.CompletedTask;
        };

        var middleware = new PermissionsAuthorizationMiddleware(
            NullLogger<PermissionsAuthorizationMiddleware>.Instance,
            new StubPermissionEvaluator(false));

        var context = new DefaultHttpContext();

        await middleware.InvokeAsync(context, next);

        called.Should().BeFalse();
        context.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    private sealed class StubPermissionEvaluator : IRequestPermissionEvaluator
    {
        private readonly bool _result;

        public StubPermissionEvaluator(bool result)
        {
            _result = result;
        }

        public Task<bool> IsAuthorizedAsync(HttpContext httpContext, CancellationToken cancellationToken)
            => Task.FromResult(_result);
    }
}
