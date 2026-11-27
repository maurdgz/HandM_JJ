namespace ImportNet.Infrastructure.ExternalServices;

public sealed class IaSettings
{
    public const string SectionName = "Ia";

    public string Provider { get; set; } = "Gemini";
    public string ApiKey { get; set; } = "";
    public string Model { get; set; } = "gemini-2.5-flash";
    public string? BaseUrl { get; set; }
    public int TimeoutSeconds { get; set; } = 30;
}
