using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace KoreForge.Web.Authorization.Tests.TestHelpers;

internal static class ControllerEndpointFactory
{
    public static HttpContext CreateContext(
        Type controllerType,
        string methodName,
        string[] roles,
        bool authenticated = true)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.User = authenticated
            ? ClaimsPrincipalFactory.Create(roles)
            : new ClaimsPrincipal(new ClaimsIdentity());

        var methodInfo = controllerType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance)
                         ?? throw new InvalidOperationException($"Method {methodName} not found on {controllerType.FullName}.");

        var actionDescriptor = new ControllerActionDescriptor
        {
            ControllerTypeInfo = controllerType.GetTypeInfo(),
            MethodInfo = methodInfo,
            ControllerName = controllerType.Name,
            ActionName = methodInfo.Name
        };

        var endpoint = new Endpoint(
            _ => Task.CompletedTask,
            new EndpointMetadataCollection(actionDescriptor),
            displayName: methodName);

        httpContext.SetEndpoint(endpoint);
        httpContext.RequestServices = new ServiceCollection().BuildServiceProvider();

        return httpContext;
    }
}
