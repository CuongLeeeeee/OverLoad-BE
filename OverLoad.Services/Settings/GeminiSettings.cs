namespace OverLoad.Services.Settings;

/// <summary>Bound from the "Gemini" configuration section.</summary>
public class GeminiSettings
{
    public const string SectionName = "Gemini";

    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gemini-2.0-flash";
    public int MaxOutputTokens { get; set; } = 2048;
    public double Temperature { get; set; } = 0.7;
}
