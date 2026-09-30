using System.Security.Cryptography;
using OverLoad.Repositories.Interfaces;

namespace OverLoad.Services.Common;

/// <summary>
/// Generates PayOS order codes: unix milliseconds × 1000 plus a random 0-999 suffix, so two
/// orders created in the same millisecond still differ. The result stays below 2^53, the
/// largest order code PayOS accepts. The unique index on order_code is the final guard.
/// </summary>
internal static class OrderCodes
{
    private const int MaxAttempts = 5;

    public static async Task<long> NewAsync(ITransactionRepository transactions)
    {
        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            var code = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000 + RandomNumberGenerator.GetInt32(1000);
            if (!await transactions.OrderCodeExistsAsync(code))
                return code;
        }

        throw new InvalidOperationException("Could not generate a unique order code.");
    }
}
