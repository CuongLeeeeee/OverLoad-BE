namespace OverLoad.API.Common;

/// <summary>Bound from the "PayOS" configuration section.</summary>
public class PayOSSettings
{
    public const string SectionName = "PayOS";

    public string ClientId { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ChecksumKey { get; set; } = string.Empty;
}
