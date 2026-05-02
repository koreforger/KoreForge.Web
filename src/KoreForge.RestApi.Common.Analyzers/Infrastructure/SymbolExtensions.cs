using Microsoft.CodeAnalysis;

namespace KoreForge.RestApi.Common.Analyzers.Infrastructure;

internal static class SymbolExtensions
{
    public static bool MatchesMetadataName(this ITypeSymbol? symbol, string metadataName)
    {
        return symbol?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == metadataName;
    }
}
