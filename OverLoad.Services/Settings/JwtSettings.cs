namespace OverLoad.Services.Settings;

/// <summary>Bound from the "Jwt" configuration section.</summary>
public class JwtSettings
{
    public const string SectionName = "Jwt";

    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = "OverLoadAPI";
    public string Audience { get; set; } = "OverLoadClient";
    public int AccessTokenExpiryMinutes { get; set; } = 60;
    public int RefreshTokenExpiryDays { get; set; } = 7;
}
