using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace KoreForge.Web.Authorization.Dynamic;

/// <summary>
/// Registration + pipeline helpers for dynamic method authorization.
/// </summary>
public static class DynamicAuthorizationExtensions
{
    /// <summary>
    /// Registers the in-memory rule store, evaluator, and middleware services for dynamic authorization.
    /// </summary>
    /// <param name="services">Application service collection.</param>
    /// <param name="rules">Set of rules describing controller requirements.</param>
    /// <returns>The original service collection.</returns>
    public static IServiceCollection AddDynamicMethodAuthorization(
        this IServiceCollection services,
        IEnumerable<MethodPermissionRule> rules)
    {
        if (services is null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (rules is null)
        {
            throw new ArgumentNullException(nameof(rules));
        }

        services.AddSingleton<IMethodPermissionStore>(_ => new InMemoryMethodPermissionStore(rules));
        services.AddScoped<IRequestPermissionEvaluator, DefaultRequestPermissionEvaluator>();
        services.AddScoped<PermissionsAuthorizationMiddleware>();
        return services;
    }

    /// <summary>
    /// Adds <see cref="PermissionsAuthorizationMiddleware"/> to the pipeline.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The same application builder for chaining.</returns>
    public static IApplicationBuilder UseDynamicMethodAuthorization(this IApplicationBuilder app)
    {
        if (app is null)
        {
            throw new ArgumentNullException(nameof(app));
        }

        return app.UseMiddleware<PermissionsAuthorizationMiddleware>();
    }
}
