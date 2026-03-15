using System;
using System.Collections.Immutable;
using System.Linq;
using KF.RestApi.Common.Analyzers.Diagnostics;
using KF.RestApi.Common.Analyzers.Infrastructure;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace KF.RestApi.Common.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ApiGatewayAnalyzer : DiagnosticAnalyzer
{
    private static readonly ImmutableArray<DiagnosticDescriptor> Supported = ImmutableArray.Create(
        DiagnosticDescriptors.ExternalTypesMustBeInternal,
        DiagnosticDescriptors.ExternalCannotReferenceOtherLayers,
        DiagnosticDescriptors.DomainCannotReferenceDisallowedLayers,
        DiagnosticDescriptors.InternalCannotReferenceExternal,
        DiagnosticDescriptors.ExternalMethodsMustReturnApiResponse,
        DiagnosticDescriptors.ExternalMethodsRequireCancellationToken,
        DiagnosticDescriptors.RestrictedHttpClientUsage);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => Supported;

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(StartAnalysis);
    }

    private static void StartAnalysis(CompilationStartAnalysisContext context)
    {
        var state = AnalyzerState.Create(context.Compilation);
        if (state.Layer == GatewayLayer.Other)
        {
            return;
        }

        context.RegisterCompilationEndAction(ctx => AnalyzeLayerReferences(ctx, state));

        if (state.Layer == GatewayLayer.External)
        {
            context.RegisterSymbolAction(ctx => AnalyzeExternalTypeAccessibility(ctx, state), SymbolKind.NamedType);
            context.RegisterSymbolAction(ctx => AnalyzeExternalMethodContracts(ctx, state), SymbolKind.Method);
        }

        if (state.Layer != GatewayLayer.Client)
        {
            context.RegisterSyntaxNodeAction(ctx => AnalyzeHttpClientUsage(ctx, state), SyntaxKind.ObjectCreationExpression);
        }
    }

    private static void AnalyzeExternalTypeAccessibility(SymbolAnalysisContext context, AnalyzerState state)
    {
        if (state.Layer != GatewayLayer.External)
        {
            return;
        }

        if (context.Symbol is not INamedTypeSymbol typeSymbol)
        {
            return;
        }

        if (typeSymbol.DeclaredAccessibility == Accessibility.Public || typeSymbol.DeclaredAccessibility == Accessibility.Protected || typeSymbol.DeclaredAccessibility == Accessibility.ProtectedOrInternal)
        {
            var location = typeSymbol.Locations.FirstOrDefault() ?? Location.None;
            context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.ExternalTypesMustBeInternal, location, typeSymbol.Name));
        }
    }

    private static void AnalyzeExternalMethodContracts(SymbolAnalysisContext context, AnalyzerState state)
    {
        if (state.Layer != GatewayLayer.External)
        {
            return;
        }

        if (context.Symbol is not IMethodSymbol method || method.MethodKind != MethodKind.Ordinary)
        {
            return;
        }

        if (method.ContainingType.TypeKind != TypeKind.Interface)
        {
            return;
        }

        var location = method.Locations.FirstOrDefault() ?? Location.None;

        if (!ReturnsApiResponseTask(method.ReturnType, state))
        {
            context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.ExternalMethodsMustReturnApiResponse, location, method.Name));
        }

        if (!HasTrailingCancellationToken(method, state))
        {
            context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.ExternalMethodsRequireCancellationToken, location, method.Name));
        }
    }

    private static bool ReturnsApiResponseTask(ITypeSymbol returnType, AnalyzerState state)
    {
        if (returnType is not INamedTypeSymbol namedType)
        {
            return false;
        }

        if (!namedType.IsGenericType)
        {
            return false;
        }

        if (namedType.ConstructedFrom is not INamedTypeSymbol taskOfT || !SymbolEqualityComparer.Default.Equals(taskOfT, state.TaskOfTSymbol))
        {
            return false;
        }

        var target = namedType.TypeArguments[0];
        if (target is not INamedTypeSymbol apiResponse)
        {
            return false;
        }

        if (SymbolEqualityComparer.Default.Equals(apiResponse, state.ApiResponseSymbol))
        {
            return true;
        }

        if (apiResponse.IsGenericType && SymbolEqualityComparer.Default.Equals(apiResponse.ConstructedFrom, state.ApiResponseOfTSymbol))
        {
            return true;
        }

        var ns = apiResponse.ContainingNamespace?.ToDisplayString();
        return apiResponse.Name == "ApiResponse" && string.Equals(ns, "Refit", StringComparison.Ordinal);
    }

    private static bool HasTrailingCancellationToken(IMethodSymbol method, AnalyzerState state)
    {
        if (method.Parameters.Length == 0)
        {
            return false;
        }

        var last = method.Parameters[method.Parameters.Length - 1];
        return last.Type.MatchesMetadataName("global::System.Threading.CancellationToken") || SymbolEqualityComparer.Default.Equals(last.Type, state.CancellationTokenSymbol);
    }

    private static void AnalyzeLayerReferences(CompilationAnalysisContext context, AnalyzerState state)
    {
        var compilation = context.Compilation;
        var assemblyName = compilation.AssemblyName ?? "<unknown>";

        foreach (var reference in compilation.ReferencedAssemblyNames)
        {
            if (state.Layer == GatewayLayer.External && IsDisallowed(reference.Name, "Domain", "Internal", "Client"))
            {
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.ExternalCannotReferenceOtherLayers, LayerMetadata.GetProjectLocation(compilation), assemblyName, reference.Name));
            }
            else if (state.Layer == GatewayLayer.Domain && IsDisallowed(reference.Name, "Internal", "Client"))
            {
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.DomainCannotReferenceDisallowedLayers, LayerMetadata.GetProjectLocation(compilation), assemblyName, reference.Name));
            }
            else if (state.Layer == GatewayLayer.Internal && IsDisallowed(reference.Name, "External"))
            {
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.InternalCannotReferenceExternal, LayerMetadata.GetProjectLocation(compilation), assemblyName, reference.Name));
            }
        }
    }

    private static bool IsDisallowed(string name, params string[] prefixes)
    {
        foreach (var prefix in prefixes)
        {
            var trimmed = prefix.EndsWith(".", StringComparison.Ordinal)
                ? prefix.Substring(0, prefix.Length - 1)
                : prefix;

            if (LayerMetadata.HasLayerPrefix(name, trimmed))
            {
                return true;
            }
        }

        return false;
    }

    private static void AnalyzeHttpClientUsage(SyntaxNodeAnalysisContext context, AnalyzerState state)
    {
        if (state.Layer == GatewayLayer.Client)
        {
            return;
        }

        if (context.Node is not ObjectCreationExpressionSyntax objectCreation)
        {
            return;
        }

        var type = context.SemanticModel.GetTypeInfo(objectCreation, context.CancellationToken).Type;
        if (type is null)
        {
            return;
        }

        if (type.MatchesMetadataName("global::System.Net.Http.HttpClient"))
        {
            context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.RestrictedHttpClientUsage, objectCreation.GetLocation(), state.Layer.ToString()));
        }
    }
}
