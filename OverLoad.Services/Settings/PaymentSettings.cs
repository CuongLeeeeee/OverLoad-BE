namespace OverLoad.Services.Settings;

/// <summary>Bound from the "Payment" configuration section.</summary>
public class PaymentSettings
{
    public const string SectionName = "Payment";

    /// <summary>Frontend origin. Return/cancel URLs sent by clients must start with it.</summary>
    public string FrontendBaseUrl { get; set; } = "http://localhost:3000";
    public string ReturnPath { get; set; } = "/payment/success";
    public string CancelPath { get; set; } = "/payment/cancel";

    public decimal ProMonthPrice { get; set; } = 69000;
    public decimal ProYearPrice { get; set; } = 599000;

    public decimal MinDepositAmount { get; set; } = 10000;
    public decimal MaxDepositAmount { get; set; } = 50000000;
}
