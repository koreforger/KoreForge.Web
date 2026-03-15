using System.ComponentModel.DataAnnotations;

namespace KF.RestApi.Common.Persistence.Options;

/// <summary>
/// Controls how sensitive fields are redacted before persisting audit payloads.
/// </summary>
public sealed class AuditRedactionOptions
{
    public const string ConfigurationSection = "Persistence:Audit:Redaction";

    /// <summary>
    /// Case-insensitive list of JSON property names to redact.
    /// </summary>
    [Required]
    public IReadOnlyList<string> SensitiveKeys { get; init; } = new List<string>
    {
        "token",
        "access_token",
        "refresh_token",
        "password",
        "secret",
        "authorization",
        "bearerToken"
    };

    /// <summary>
    /// Replacement string written to payloads for redacted fields.
    /// </summary>
    [Required]
    [MinLength(3)]
    public string Replacement { get; init; } = "***REDACTED***";
}