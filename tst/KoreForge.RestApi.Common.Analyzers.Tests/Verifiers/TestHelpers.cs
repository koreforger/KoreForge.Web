using System;
using System.Collections.Generic;
using System.Linq;
using KoreForge.RestApi.Common.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;

namespace KoreForge.RestApi.Common.Analyzers.Tests.Verifiers;

internal static class TestHelpers
{
    public static CSharpAnalyzerTest<ApiGatewayAnalyzer, XUnitVerifier> CreateAnalyzerTest(
        string assemblyName,
        IDictionary<string, string>? additionalAssemblyNames = null)
    {
        var test = new CSharpAnalyzerTest<ApiGatewayAnalyzer, XUnitVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
        };

        ApplyAssemblyNameTransform(test.SolutionTransforms, assemblyName, additionalAssemblyNames);
        return test;
    }

    public static CSharpCodeFixTest<ApiGatewayAnalyzer, TCodeFix, XUnitVerifier> CreateCodeFixTest<TCodeFix>(
        string assemblyName,
        IDictionary<string, string>? additionalAssemblyNames = null)
        where TCodeFix : CodeFixProvider, new()
    {
        var test = new CSharpCodeFixTest<ApiGatewayAnalyzer, TCodeFix, XUnitVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
        };

        ApplyAssemblyNameTransform(test.SolutionTransforms, assemblyName, additionalAssemblyNames);
        return test;
    }

    private static void ApplyAssemblyNameTransform(
        IList<Func<Solution, ProjectId, Solution>> transforms,
        string assemblyName,
        IDictionary<string, string>? additionalAssemblyNames)
    {
        transforms.Add((solution, projectId) =>
        {
            solution = solution.WithProjectAssemblyName(projectId, assemblyName);

            if (additionalAssemblyNames is not null)
            {
                foreach (var mapping in additionalAssemblyNames)
                {
                    var targetProject = solution.Projects.FirstOrDefault(p => p.Name == mapping.Key);
                    if (targetProject is not null)
                    {
                        solution = solution.WithProjectAssemblyName(targetProject.Id, mapping.Value);
                    }
                }
            }

            return solution;
        });
    }
}
