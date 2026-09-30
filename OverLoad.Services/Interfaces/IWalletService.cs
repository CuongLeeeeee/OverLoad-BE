using OverLoad.Domain.Enums;
using OverLoad.Services.DTOs.Response;

namespace OverLoad.Services.Interfaces;

public interface IWalletService
{
    /// <summary>Balance and PRO status. Pending deposits are refreshed from the gateway first.</summary>
    Task<WalletResponse> GetWalletAsync(int userId);

    /// <summary>
    /// Buys a PRO package with the wallet balance. The debit, the PRO extension and the
    /// transaction record are saved atomically; concurrent purchases cannot overdraw the wallet.
    /// </summary>
    Task<WalletResponse> PurchaseProAsync(int userId, ProPackage package);
}
