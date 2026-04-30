using Microsoft.Extensions.DependencyInjection;

namespace KF.Web.Authorization.Core;

/// <summary>
/// Registration helpers for role-authorization services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the core role-authorization services required by the library.
    /// </summary>
    /// <param name="services">The target service collection.</param>
    /// <returns>The original service collection.</returns>
    public static IServiceCollection AddRoleAuthorizationCore(this IServiceCollection services)
    {
        if (services is null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        services.AddScoped<IRoleAuthorizationService, RoleAuthorizationService>();
        return services;
    }
}
