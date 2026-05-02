using Microsoft.CodeAnalysis;

namespace KoreForge.RestApi.Common.Analyzers.Diagnostics;

internal static class DiagnosticDescriptors
{
    private const string CategoryArchitecture = "Architecture";
    private const string CategoryHttpContract = "HttpContract";
    private static readonly string[] CompilationEndTags = { WellKnownDiagnosticTags.CompilationEnd };

    internal static readonly DiagnosticDescriptor ExternalTypesMustBeInternal = new(
        id: "API001",
        title: "External layer types must be internal",
        messageFormat: "Type '{0}' must be declared internal inside External.* or KoreForge.RestApi.External.* projects",
        category: CategoryArchitecture,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Prevents leaking provider DTOs or transports beyond the External layer.");

    internal static readonly DiagnosticDescriptor ExternalCannotReferenceOtherLayers = new(
        id: "API002",
        title: "External layer cannot reference Domain/Internal/Client projects",
        messageFormat: "Project '{0}' must not reference '{1}'",
        category: CategoryArchitecture,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "External layer should remain isolated from Domain/Internal/Client assemblies.",
        customTags: CompilationEndTags);

    internal static readonly DiagnosticDescriptor DomainCannotReferenceDisallowedLayers = new(
        id: "API003",
        title: "Domain layer cannot reference Internal or Client projects",
        messageFormat: "Domain project '{0}' must not reference '{1}'",
        category: CategoryArchitecture,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Domain layer should not take dependencies on Internal or Client assemblies.",
        customTags: CompilationEndTags);

    internal static readonly DiagnosticDescriptor InternalCannotReferenceExternal = new(
        id: "API004",
        title: "Internal layer cannot reference External projects",
        messageFormat: "Internal project '{0}' must not reference '{1}'",
        category: CategoryArchitecture,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Internal HTTP surface must route via Domain and never bind to External transports.",
        customTags: CompilationEndTags);

    internal static readonly DiagnosticDescriptor ExternalMethodsMustReturnApiResponse = new(
        id: "API005",
        title: "Refit interface methods must return Task<ApiResponse<T>>",
        messageFormat: "Method '{0}' must return Task<Refit.ApiResponse<T>>",
        category: CategoryHttpContract,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Standardizes External Refit signatures so downstream layers can inspect transport metadata.");

    internal static readonly DiagnosticDescriptor ExternalMethodsRequireCancellationToken = new(
        id: "API006",
        title: "Refit methods must include a trailing CancellationToken",
        messageFormat: "Method '{0}' must accept a trailing CancellationToken parameter",
        category: CategoryHttpContract,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "External calls must expose CancellationToken for cooperative cancellation.");

    internal static readonly DiagnosticDescriptor RestrictedHttpClientUsage = new(
        id: "API007",
        title: "HttpClient usage is restricted",
        messageFormat: "Layer '{0}' must not create HttpClient instances directly",
        category: CategoryArchitecture,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Only Client.* or KoreForge.RestApi.Client.* modules may allocate HttpClient; other layers must use prescribed abstractions.");
}
