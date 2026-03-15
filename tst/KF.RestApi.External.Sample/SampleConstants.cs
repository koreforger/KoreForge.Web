namespace KF.RestApi.External.Sample;

/// <summary>
/// Centralizes names that must stay consistent between layers.
/// </summary>
internal static class SampleConstants
{
    public const string ApiName = "Sample";
    public const string ConfigurationSection = "Apis:" + ApiName;
    public const string HttpClientName = ApiName + "External";
}
