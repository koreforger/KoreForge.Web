using Microsoft.CodeAnalysis;

namespace KF.RestApi.Common.Analyzers.Infrastructure;

internal sealed class AnalyzerState
{
    private AnalyzerState(GatewayLayer layer)
    {
        Layer = layer;
    }

    public GatewayLayer Layer { get; }

    public INamedTypeSymbol? TaskOfTSymbol { get; private set; }

    public INamedTypeSymbol? ApiResponseSymbol { get; private set; }

    public INamedTypeSymbol? ApiResponseOfTSymbol { get; private set; }

    public INamedTypeSymbol? CancellationTokenSymbol { get; private set; }

    public INamedTypeSymbol? HttpClientSymbol { get; private set; }

    public static AnalyzerState Create(Compilation compilation)
    {
        var layer = LayerMetadata.FromAssemblyName(compilation.AssemblyName);
        var state = new AnalyzerState(layer)
        {
            TaskOfTSymbol = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1"),
            ApiResponseSymbol = compilation.GetTypeByMetadataName("Refit.ApiResponse"),
            ApiResponseOfTSymbol = compilation.GetTypeByMetadataName("Refit.ApiResponse`1"),
            CancellationTokenSymbol = compilation.GetTypeByMetadataName("System.Threading.CancellationToken"),
            HttpClientSymbol = compilation.GetTypeByMetadataName("System.Net.Http.HttpClient")
        };

        return state;
    }
}
