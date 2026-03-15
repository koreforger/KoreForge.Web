using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KF.RestApi.Common.Analyzers.Diagnostics;
using KF.RestApi.Common.Analyzers.Tests.Verifiers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace KF.RestApi.Common.Analyzers.Tests;

public sealed class ArchitectureRuleTests
{
    [Fact]
    public async Task API002_ExternalReferencingDomain_FlagsAssembly()
    {
        const string source = @"using Domain.Cases;

namespace KF.RestApi.External.Sample;

internal class Foo
{
    private readonly DomainType _type = new();
}
";

        var test = TestHelpers.CreateAnalyzerTest(
            assemblyName: "KF.RestApi.External.Sample",
            additionalAssemblyNames: new Dictionary<string, string> { ["DomainProject"] = "Domain.Cases" });
        test.TestBehaviors |= TestBehaviors.SkipSuppressionCheck;

        test.TestCode = source;
        var domainProject = test.TestState.AdditionalProjects["DomainProject"];
        domainProject.ReferenceAssemblies = ReferenceAssemblies.Net.Net60;
        domainProject.Sources.Add("namespace Domain.Cases { public class DomainType { } }");

        test.SolutionTransforms.Add((solution, projectId) =>
        {
            var domainProject = solution.Projects.First(p => p.Name == "DomainProject");
            solution = solution.AddProjectReference(projectId, new ProjectReference(domainProject.Id));
            return solution;
        });

        var expected = new DiagnosticResult(DiagnosticDescriptors.ExternalCannotReferenceOtherLayers)
            .WithSpan(1, 1, 1, 1)
            .WithArguments("KF.RestApi.External.Sample", "Domain.Cases");
        test.ExpectedDiagnostics.Add(expected);

        await test.RunAsync();
    }

    [Fact]
    public async Task API003_DomainReferencingClient_FlagsAssembly()
    {
        const string source = @"using Client.Cases;

namespace KF.RestApi.Domain.Sample;

internal class DomainHandler
{
    private readonly ClientFacade _client = new();
}
";

        var test = TestHelpers.CreateAnalyzerTest(
            assemblyName: "KF.RestApi.Domain.Sample",
            additionalAssemblyNames: new Dictionary<string, string> { ["ClientProject"] = "Client.Cases" });
        test.TestBehaviors |= TestBehaviors.SkipSuppressionCheck;

        test.TestCode = source;
        var clientProject = test.TestState.AdditionalProjects["ClientProject"];
        clientProject.ReferenceAssemblies = ReferenceAssemblies.Net.Net60;
        clientProject.Sources.Add("namespace Client.Cases { public class ClientFacade { } }");

        test.SolutionTransforms.Add((solution, projectId) =>
        {
            var clientProject = solution.Projects.First(p => p.Name == "ClientProject");
            solution = solution.AddProjectReference(projectId, new ProjectReference(clientProject.Id));
            return solution;
        });

        var expected = new DiagnosticResult(DiagnosticDescriptors.DomainCannotReferenceDisallowedLayers)
            .WithSpan(1, 1, 1, 1)
            .WithArguments("KF.RestApi.Domain.Sample", "Client.Cases");
        test.ExpectedDiagnostics.Add(expected);

        await test.RunAsync();
    }

    [Fact]
    public async Task API004_InternalReferencingExternal_FlagsAssembly()
    {
        const string source = @"using External.Cases;

namespace KF.RestApi.Internal.Sample;

internal class InternalEndpoint
{
    private readonly ExternalFacade _facade = new();
}
";

        var test = TestHelpers.CreateAnalyzerTest(
            assemblyName: "KF.RestApi.Internal.Sample",
            additionalAssemblyNames: new Dictionary<string, string> { ["ExternalProject"] = "External.Cases" });
        test.TestBehaviors |= TestBehaviors.SkipSuppressionCheck;

        test.TestCode = source;
        var externalProject = test.TestState.AdditionalProjects["ExternalProject"];
        externalProject.ReferenceAssemblies = ReferenceAssemblies.Net.Net60;
        externalProject.Sources.Add(@"
    using System.Runtime.CompilerServices;
    [assembly: InternalsVisibleTo(""KF.RestApi.Internal.Sample"")]

    namespace External.Cases
    {
        internal class ExternalFacade { }
    }
    ");

        test.SolutionTransforms.Add((solution, projectId) =>
        {
            var externalProject = solution.Projects.First(p => p.Name == "ExternalProject");
            solution = solution.AddProjectReference(projectId, new ProjectReference(externalProject.Id));
            return solution;
        });

        var expected = new DiagnosticResult(DiagnosticDescriptors.InternalCannotReferenceExternal)
            .WithSpan(1, 1, 1, 1)
            .WithArguments("KF.RestApi.Internal.Sample", "External.Cases");
        test.ExpectedDiagnostics.Add(expected);

        await test.RunAsync();
    }

    [Fact]
    public async Task API007_DomainHttpClientInstantiation_Warns()
    {
        const string source = @"using System.Net.Http;

namespace KF.RestApi.Domain.Sample;

internal class HttpCaller
{
    public HttpClient Create() => new HttpClient();
}
";

        var test = TestHelpers.CreateAnalyzerTest("KF.RestApi.Domain.Sample");
        test.TestCode = source;

        var expected = new DiagnosticResult(DiagnosticDescriptors.RestrictedHttpClientUsage)
            .WithSpan(7, 35, 7, 51)
            .WithArguments("Domain");

        test.ExpectedDiagnostics.Add(expected);
        await test.RunAsync();
    }

    [Fact]
    public async Task API007_ClientHttpClientInstantiation_IsAllowed()
    {
        const string source = @"using System.Net.Http;

namespace KF.RestApi.Client.Sample;

internal class ClientCaller
{
    public HttpClient Create() => new HttpClient();
}
";

        var test = TestHelpers.CreateAnalyzerTest("KF.RestApi.Client.Sample");
        test.TestCode = source;
        await test.RunAsync();
    }
}
