using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KF.RestApi.Common.Analyzers.CodeFixes;
using KF.RestApi.Common.Analyzers.Diagnostics;
using KF.RestApi.Common.Analyzers.Tests.Verifiers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace KF.RestApi.Common.Analyzers.Tests;

public sealed class ExternalLayerAnalyzerTests
{
    [Fact]
    public async Task API001_PublicType_ProducesDiagnosticAndFix()
    {
        const string source = @"
namespace KF.RestApi.External.Sample
{
    public class TransportModel
    {
    }
}
";

        const string fixedSource = @"
namespace KF.RestApi.External.Sample
{
    internal class TransportModel
    {
    }
}
";

        var expected = new DiagnosticResult(DiagnosticDescriptors.ExternalTypesMustBeInternal)
            .WithSpan(4, 18, 4, 32)
            .WithArguments("TransportModel");

        var test = TestHelpers.CreateCodeFixTest<MakeTypeInternalCodeFixProvider>("KF.RestApi.External.Sample");
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ExpectedDiagnostics.Add(expected);

        await test.RunAsync();
    }

    [Fact]
    public async Task API005_ReturningTaskOfModel_IsFlagged()
    {
        const string source = @"using System.Threading;
using System.Threading.Tasks;
using Refit;

namespace KF.RestApi.External.Sample
{
    internal interface IExampleApi
    {
        Task<ExampleResponse> GetAsync(CancellationToken cancellationToken);
    }

    internal sealed class ExampleResponse { }
}
";

        const string fixedSource = @"using System.Threading;
using System.Threading.Tasks;
using Refit;

namespace KF.RestApi.External.Sample
{
    internal interface IExampleApi
    {
        global::System.Threading.Tasks.Task<global::Refit.ApiResponse<object>> GetAsync(CancellationToken cancellationToken);
    }

    internal sealed class ExampleResponse { }
}
";

        var expected = new DiagnosticResult(DiagnosticDescriptors.ExternalMethodsMustReturnApiResponse)
            .WithSpan(9, 31, 9, 39)
            .WithArguments("GetAsync");

        var test = TestHelpers.CreateCodeFixTest<RefitReturnTypeCodeFixProvider>(
            "KF.RestApi.External.Sample",
            new Dictionary<string, string> { ["RefitProject"] = "Refit" });
        test.TestCode = source;
        test.FixedCode = fixedSource;

        var refitProject = test.TestState.AdditionalProjects["RefitProject"];
        refitProject.ReferenceAssemblies = ReferenceAssemblies.Net.Net60;
        refitProject.Sources.Add(@"namespace Refit { public class ApiResponse { } public class ApiResponse<T> { } }");

        test.SolutionTransforms.Add((solution, projectId) =>
        {
            var refitProject = solution.Projects.First(p => p.Name == "RefitProject");
            solution = solution.AddProjectReference(projectId, new ProjectReference(refitProject.Id));
            return solution;
        });

        test.ExpectedDiagnostics.Add(expected);

        await test.RunAsync();
    }

    [Fact]
    public async Task API006_MissingCancellationToken_IsFlagged()
    {
        const string source = @"using System.Threading.Tasks;
using Refit;

namespace KF.RestApi.External.Sample
{
    internal interface IExampleApi
    {
        Task<ApiResponse> GetAsync();
    }
}
";

        var expected = new DiagnosticResult(DiagnosticDescriptors.ExternalMethodsRequireCancellationToken)
            .WithSpan(8, 27, 8, 35)
            .WithArguments("GetAsync");

        var test = TestHelpers.CreateAnalyzerTest(
            "KF.RestApi.External.Sample",
            new Dictionary<string, string> { ["RefitProject"] = "Refit" });
        test.TestCode = source;

        var refitProject = test.TestState.AdditionalProjects["RefitProject"];
        refitProject.ReferenceAssemblies = ReferenceAssemblies.Net.Net60;
        refitProject.Sources.Add(@"namespace Refit { public class ApiResponse { } }");

        test.SolutionTransforms.Add((solution, projectId) =>
        {
            var refitProject = solution.Projects.First(p => p.Name == "RefitProject");
            solution = solution.AddProjectReference(projectId, new ProjectReference(refitProject.Id));
            return solution;
        });

        test.ExpectedDiagnostics.Add(expected);

        await test.RunAsync();
    }
}
