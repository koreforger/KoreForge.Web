using KoreForge.RestApi.Client.Sample.Services;
using KoreForge.RestApi.Domain.Sample.Services;
using NetArchTest.Rules;
using Xunit;
using InternalDi = KoreForge.RestApi.Internal.Sample.DependencyInjection.ServiceCollectionExtensions;

namespace KoreForge.RestApi.Architecture.Sample.Tests;

public sealed class LayeringRules
{
    [Fact]
    public void DomainDependsOnExternal()
    {
        var result = Types.InAssembly(typeof(ISampleService).Assembly)
            .That().AreClasses()
            .And().ResideInNamespace("KoreForge.RestApi.Domain.Sample.Services")
            .Should().HaveDependencyOn("KoreForge.RestApi.External.Sample")
            .GetResult();

        AssertSuccessful(result, "Domain must reference External.");
    }

    [Fact]
    public void InternalDoesNotReferenceExternal()
    {
        var result = Types.InAssembly(typeof(InternalDi).Assembly)
            .Should().NotHaveDependencyOn("KoreForge.RestApi.External.Sample")
            .GetResult();

        AssertSuccessful(result, "Internal cannot depend on External.");
    }

    [Fact]
    public void ClientDoesNotReferenceDomainOrExternal()
    {
        var result = Types.InAssembly(typeof(ISampleClient).Assembly)
            .Should().NotHaveDependencyOnAny(new[]
            {
                "KoreForge.RestApi.Domain.Sample",
                "KoreForge.RestApi.External.Sample"
            })
            .GetResult();

        AssertSuccessful(result, "Client boundary violated.");
    }

    private static void AssertSuccessful(TestResult result, string message)
    {
        if (result.IsSuccessful)
        {
            return;
        }

        var failingTypes = string.Join(", ", result.FailingTypes);
        throw new Xunit.Sdk.XunitException($"{message} Violations: {failingTypes}");
    }
}
