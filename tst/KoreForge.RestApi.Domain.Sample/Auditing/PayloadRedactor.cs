using System.Text.Json;
using System.Text.Json.Nodes;
using KoreForge.RestApi.Common.Persistence.Options;

namespace KoreForge.RestApi.Domain.Sample.Auditing;

/// <summary>
/// Minimal JSON redaction utility for audit payloads.
/// </summary>
internal static class PayloadRedactor
{
    public static string Redact(string? json, AuditRedactionOptions options)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return string.Empty;
        }

        try
        {
            var node = JsonNode.Parse(json);
            if (node is null)
            {
                return json;
            }

            var keys = new HashSet<string>(options.SensitiveKeys, StringComparer.OrdinalIgnoreCase);
            RedactNode(node, keys, options.Replacement);
            return node.ToJsonString(new JsonSerializerOptions(JsonSerializerDefaults.Web));
        }
        catch (JsonException)
        {
            // If parsing fails, return the raw payload to avoid data loss.
            return json;
        }
    }

    private static void RedactNode(JsonNode node, HashSet<string> sensitiveKeys, string replacement)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach (var property in obj.ToList())
                {
                    if (sensitiveKeys.Contains(property.Key))
                    {
                        obj[property.Key] = replacement;
                    }
                    else if (property.Value is JsonNode child)
                    {
                        RedactNode(child, sensitiveKeys, replacement);
                    }
                }
                break;
            case JsonArray array:
                foreach (var child in array)
                {
                    if (child is JsonNode childNode)
                    {
                        RedactNode(childNode, sensitiveKeys, replacement);
                    }
                }
                break;
        }
    }
}