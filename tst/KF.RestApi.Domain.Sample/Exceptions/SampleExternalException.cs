using Refit;

namespace KF.RestApi.Domain.Sample.Exceptions;

/// <summary>
/// Wraps upstream faults with contextual metadata that flows through ProblemDetails.
/// </summary>
public sealed class SampleExternalException : Exception
{
    private SampleExternalException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }

    public static SampleExternalException FromResponse<T>(
        string operation,
        ApiResponse<T> response,
        string requestJson,
        string? responseJson,
        string? correlationId)
    {
        var message = response.Error?.Message
            ?? $"{operation} returned HTTP {(int)response.StatusCode}.";

        var exception = new SampleExternalException(message, response.Error);
        var requestMessage = response.RequestMessage ?? response.Error?.RequestMessage;
        var requestUri = requestMessage?.RequestUri;

        exception.Data[nameof(operation)] = operation;
        exception.Data["Url"] = requestUri?.ToString() ?? string.Empty;
        exception.Data["RequestJson"] = requestJson;
        exception.Data["ResponseJson"] = responseJson ?? string.Empty;
        exception.Data["StatusCode"] = (int)response.StatusCode;
        exception.Data["CorrelationId"] = correlationId ?? string.Empty;

        return exception;
    }
}
