using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OverLoad.Domain.Entities;
using OverLoad.Domain.Enums;
using OverLoad.Repositories.Interfaces;
using OverLoad.Services.Common;
using OverLoad.Services.DTOs.Response;
using OverLoad.Services.Exceptions;
using OverLoad.Services.Interfaces;
using OverLoad.Services.Settings;

namespace OverLoad.Services.Implementations;

public class WalletService : IWalletService
{
    private readonly IUserRepository _userRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPaymentService _paymentService;
    private readonly PaymentSettings _settings;

    public WalletService(
        IUserRepository userRepository,
        ITransactionRepository transactionRepository,
        IUnitOfWork unitOfWork,
        IPaymentService paymentService,
        IOptions<PaymentSettings> settings)
    {
        _userRepository = userRepository;
        _transactionRepository = transactionRepository;
        _unitOfWork = unitOfWork;
        _paymentService = paymentService;
        _settings = settings.Value;
    }

    public async Task<WalletDto> GetWalletAsync(int userId)
    {
        await _paymentService.SyncPendingAsync(userId);
        return MapToResponse(await GetUserAsync(userId));
    }

    public async Task<WalletDto> PurchaseProAsync(int userId, ProPackage package)
    {
        // Credit deposits that were paid but whose webhook has not arrived yet.
        await _paymentService.SyncPendingAsync(userId);
        var price = package == ProPackage.Year ? _settings.ProYearPrice : _settings.ProMonthPrice;

        for (var attempt = 1; ; attempt++)
        {
            var user = await GetUserAsync(userId);
            if (user.WalletBalance < price)
                throw new BusinessRuleException(
                    $"Insufficient balance: {user.WalletBalance:N0} VND available, {price:N0} VND required.");

            var now = DateTime.UtcNow;
            var orderCode = await OrderCodes.NewAsync(_transactionRepository);

            // WalletBalance is a concurrency token, so this debit only succeeds if nobody else
            // changed the balance since it was read above.
            user.WalletBalance -= price;
            user.ExtendPro(package, now);
            _transactionRepository.Add(new Transaction
            {
                TransactionId = $"WALLET-{orderCode}",
                OrderCode = orderCode,
                UserId = userId,
                Type = TransactionType.ProUpgrade,
                Method = PaymentMethod.Wallet,
                Status = TransactionStatus.Success,
                ProPackage = package,
                Amount = price,
                CreatedAt = now,
                PaidAt = now
            });

            try
            {
                await _unitOfWork.SaveChangesAsync();
                return MapToResponse(user);
            }
            catch (DbUpdateConcurrencyException) when (attempt < PaymentService.MaxConcurrencyAttempts)
            {
                _unitOfWork.ClearChanges();   // re-read the balance and try again
            }
        }
    }

    private async Task<User> GetUserAsync(int userId)
        => await _userRepository.GetByIdAsync(userId) ?? throw new NotFoundException("User", userId);

    private static WalletDto MapToResponse(User user) => new()
    {
        Balance = user.WalletBalance,
        IsPro = user.HasActivePro(DateTime.UtcNow),
        ProExpiresAt = user.ProExpiresAt
    };
}
