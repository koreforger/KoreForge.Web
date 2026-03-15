using System.Collections.ObjectModel;
using System.Net;
using KF.RestApi.Client.Sample.Models;

namespace KF.RestApi.Client.Sample.Exceptions;

/// <summary>
/// Wraps ProblemDetails emitted by the Internal host.
/// </summary>
public sealed class InternalApiException : Exception
{
    private InternalApiException(
        string message,
        HttpStatusCode statusCode,
        string? correlationId,
        IReadOnlyDictionary<string, object?> extensions)
        : base(message)
    {
        StatusCode = statusCode;
        CorrelationId = correlationId;
        Extensions = extensions;
    }

    public HttpStatusCode StatusCode { get; }

    public string? CorrelationId { get; }

    public IReadOnlyDictionary<string, object?> Extensions { get; }

    public static InternalApiException FromProblem(HttpStatusCode statusCode, ProblemDetailsPayload? problem)
    {
        var message = problem?.Title ?? "Internal API call failed.";
        var correlationId = TryGet(problem?.Extensions, "correlationId");

        var extensionSource = problem?.Extensions
            ?? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        var extensions = new ReadOnlyDictionary<string, object?>(extensionSource);

        return new InternalApiException(message, statusCode, correlationId, extensions);
    }

    private static string? TryGet(IDictionary<string, object?>? extensions, string key)
    {
        if (extensions is null)
        {
            return null;
        }

        return extensions.TryGetValue(key, out var value)
            ? value?.ToString()
            : null;
    }
}
