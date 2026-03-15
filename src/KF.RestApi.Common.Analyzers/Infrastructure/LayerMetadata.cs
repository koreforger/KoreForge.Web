using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace KF.RestApi.Common.Analyzers.Infrastructure;

internal enum GatewayLayer
{
    External,
    Domain,
    Internal,
    Client,
    Other
}

internal static class LayerMetadata
{
    public static GatewayLayer FromAssemblyName(string? assemblyName)
    {
        if (string.IsNullOrWhiteSpace(assemblyName))
        {
            return GatewayLayer.Other;
        }

        var name = assemblyName!;

        if (HasLayerPrefix(name, "External"))
        {
            return GatewayLayer.External;
        }

        if (HasLayerPrefix(name, "Domain"))
        {
            return GatewayLayer.Domain;
        }

        if (HasLayerPrefix(name, "Internal"))
        {
            return GatewayLayer.Internal;
        }

        if (HasLayerPrefix(name, "Client"))
        {
            return GatewayLayer.Client;
        }

        return GatewayLayer.Other;
    }

    public static Location GetProjectLocation(Compilation compilation)
    {
        var tree = compilation.SyntaxTrees.FirstOrDefault();
        if (tree is null)
        {
            return Location.None;
        }

        return Location.Create(tree, new Microsoft.CodeAnalysis.Text.TextSpan(0, 0));
    }

    internal static bool HasLayerPrefix(string name, string layer)
    {
        return name.StartsWith(layer + ".", StringComparison.Ordinal)
            || name.StartsWith("KF.RestApi." + layer + ".", StringComparison.Ordinal);
    }
}
