namespace KF.Web.Authorization.Sample.Models;

public sealed class TokenRequest
{
    public string UserName { get; set; } = string.Empty;
    public string[] Roles { get; set; } = Array.Empty<string>();
    public Dictionary<string, string> AdditionalClaims { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
