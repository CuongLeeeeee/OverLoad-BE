using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OverLoad.Domain.Entities;
using OverLoad.Domain.Enums;
using OverLoad.Repositories.Interfaces;
using OverLoad.Services.Common;
using OverLoad.Services.DTOs.Request;
using OverLoad.Services.DTOs.Response;
using OverLoad.Services.Exceptions;
using OverLoad.Services.Interfaces;
using OverLoad.Services.Settings;

namespace OverLoad.Services.Implementations;

public class PaymentService : IPaymentService
{
    // A concurrent writer (webhook vs. sync, deposit vs. wallet purchase) makes SaveChanges throw
    // DbUpdateConcurrencyException; the operation is then re-run on fresh data.
    internal const int MaxConcurrencyAttempts = 3;

    private readonly ITransactionRepository _transactionRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICourseRepository _courseRepository;
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPaymentGateway _gateway;
    private readonly PaymentSettings _settings;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        ITransactionRepository transactionRepository,
        IUserRepository userRepository,
        ICourseRepository courseRepository,
        IEnrollmentRepository enrollmentRepository,
        IUnitOfWork unitOfWork,
        IPaymentGateway gateway,
        IOptions<PaymentSettings> settings,
        ILogger<PaymentService> logger)
    {
        _transactionRepository = transactionRepository;
        _userRepository = userRepository;
        _courseRepository = courseRepository;
        _enrollmentRepository = enrollmentRepository;
        _unitOfWork = unitOfWork;
        _gateway = gateway;
        _settings = settings.Value;
        _logger = logger;
    }

    // ── Checkout ─────────────────────────────────────────────────────────────

    public async Task<CheckoutDto> CheckoutAsync(int userId, CheckoutRequest request)
    {
        var transaction = new Transaction
        {
            UserId = userId,
            Method = PaymentMethod.PayOS,
            Status = TransactionStatus.Pending,
            OrderCode = await OrderCodes.NewAsync(_transactionRepository)
        };

        string itemName;
        string description;

        switch (request.Type)
        {
            case CheckoutType.Course:
                var course = await GetPurchasableCourseAsync(userId, request.CourseId);
                transaction.Type = TransactionType.CoursePurchase;
                transaction.CourseId = course.Id;
                transaction.Amount = course.Price;
                itemName = course.Title;
                description = $"Mua KH {course.Id}";
                break;

            case CheckoutType.Pro:
                var package = request.Package ?? throw new BusinessRuleException("Package is required for a PRO checkout.");
                transaction.Type = TransactionType.ProUpgrade;
                transaction.ProPackage = package;
                transaction.Amount = ProPrice(package);
                itemName = package == ProPackage.Year ? "Goi PRO 1 nam" : "Goi PRO 1 thang";
                description = $"Pro {package}";
                break;

            case CheckoutType.Deposit:
                var amount = request.Amount ?? throw new BusinessRuleException("Amount is required for a deposit.");
                if (amount < _settings.MinDepositAmount || amount > _settings.MaxDepositAmount || amount != decimal.Truncate(amount))
                    throw new BusinessRuleException(
                        $"Deposit must be a whole amount between {_settings.MinDepositAmount:N0} and {_settings.MaxDepositAmount:N0} VND.");
                transaction.Type = TransactionType.Deposit;
                transaction.Amount = amount;
                itemName = $"Nap tien {amount:0}d";
                description = $"Nap tien {userId}";
                break;

            default:
                throw new BusinessRuleException($"Unsupported checkout type '{request.Type}'.");
        }

        var link = await _gateway.CreatePaymentLinkAsync(new GatewayPaymentLinkRequest(
            transaction.OrderCode,
            (long)transaction.Amount,
            Truncate(description, 25),
            Truncate(itemName, 25),
            ResolveFrontendUrl(request.ReturnUrl, _settings.ReturnPath),
            ResolveFrontendUrl(request.CancelUrl, _settings.CancelPath)));

        transaction.TransactionId = link.PaymentLinkId;
        _transactionRepository.Add(transaction);
        await _unitOfWork.SaveChangesAsync();

        return new CheckoutDto
        {
            OrderCode = transaction.OrderCode,
            Amount = transaction.Amount,
            CheckoutUrl = link.CheckoutUrl
        };
    }

    // ── Fulfilment ───────────────────────────────────────────────────────────

    public async Task<bool> ConfirmPaidAsync(long orderCode, long amountPaid)
    {
        for (var attempt = 1; ; attempt++)
        {
            var transaction = await _transactionRepository.GetByOrderCodeAsync(orderCode);
            if (transaction == null)
            {
                _logger.LogWarning("Payment confirmation for unknown order {OrderCode}", orderCode);
                return false;
            }

            if (transaction.Status == TransactionStatus.Success)
                return true;   // already fulfilled — webhook retries and sync land here

            if (amountPaid < transaction.Amount)
            {
                _logger.LogWarning("Order {OrderCode} underpaid: {Paid} of {Amount}", orderCode, amountPaid, transaction.Amount);
                return true;
            }

            // A Cancelled order that still gets paid is fulfilled too: the money was received.
            var now = DateTime.UtcNow;
            transaction.Status = TransactionStatus.Success;
            transaction.PaidAt = now;
            await ApplyAsync(transaction, now);

            try
            {
                await _unitOfWork.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateConcurrencyException) when (attempt < MaxConcurrencyAttempts)
            {
                _unitOfWork.ClearChanges();
            }
        }
    }

    /// <summary>Grants what was bought. Changes are saved together with the status update.</summary>
    private async Task ApplyAsync(Transaction transaction, DateTime now)
    {
        switch (transaction.Type)
        {
            case TransactionType.CoursePurchase:
                if (transaction.CourseId is int courseId
                    && !await _enrollmentRepository.IsEnrolledAsync(transaction.UserId, courseId))
                {
                    _enrollmentRepository.Add(new Enrollment
                    {
                        UserId = transaction.UserId,
                        CourseId = courseId,
                        EnrolledAt = now
                    });
                }
                break;

            case TransactionType.ProUpgrade:
                var proUser = await GetUserAsync(transaction.UserId);
                proUser.ExtendPro(transaction.ProPackage ?? ProPackage.Month, now);
                break;

            case TransactionType.Deposit:
                var depositUser = await GetUserAsync(transaction.UserId);
                depositUser.WalletBalance += transaction.Amount;
                break;
        }
    }

    public async Task SyncPendingAsync(int userId)
    {
        foreach (var pending in await _transactionRepository.GetPendingGatewayTransactionsAsync(userId))
            await SyncAsync(pending.OrderCode);
    }

    private async Task SyncAsync(long orderCode)
    {
        GatewayPaymentStatus status;
        try
        {
            status = await _gateway.GetPaymentStatusAsync(orderCode);
        }
        catch (ExternalServiceException ex)
        {
            // One unreachable order must not block the others or the caller's request.
            _logger.LogWarning(ex, "Could not refresh order {OrderCode} from the payment gateway", orderCode);
            return;
        }

        switch (status.State)
        {
            case GatewayPaymentState.Paid:
                await ConfirmPaidAsync(orderCode, status.AmountPaid);
                break;

            case GatewayPaymentState.Closed:
                var transaction = await _transactionRepository.GetByOrderCodeAsync(orderCode);
                if (transaction?.Status != TransactionStatus.Pending) return;

                transaction.Status = TransactionStatus.Cancelled;
                try
                {
                    await _unitOfWork.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    _unitOfWork.ClearChanges();   // someone else already moved it on
                }
                break;
        }
    }

    // ── Queries ──────────────────────────────────────────────────────────────

    public async Task<TransactionDto> GetMyOrderAsync(int userId, long orderCode)
    {
        var transaction = await _transactionRepository.GetByOrderCodeAsync(orderCode);
        if (transaction == null || transaction.UserId != userId)
            throw new NotFoundException("Order", orderCode);

        if (transaction.Status == TransactionStatus.Pending && transaction.Method == PaymentMethod.PayOS)
        {
            await SyncAsync(orderCode);
            _unitOfWork.ClearChanges();
            transaction = (await _transactionRepository.GetByOrderCodeAsync(orderCode))!;
        }

        return MapToResponse(transaction);
    }

    public async Task<PagedResult<TransactionDto>> GetMyTransactionsAsync(int userId, TransactionQueryParams query)
    {
        await SyncPendingAsync(userId);
        return await SearchAsync(userId, query);
    }

    public Task<PagedResult<TransactionDto>> GetAllTransactionsAsync(TransactionQueryParams query)
        => SearchAsync(query.UserId, query);

    public async Task<RevenueStatsDto> GetRevenueStatsAsync()
    {
        var totals = await _transactionRepository.GetSuccessTotalsAsync();
        var (recent, _) = await _transactionRepository.SearchAsync(
            null, null, TransactionStatus.Success, 1, 10, "createdat", true);

        decimal Sum(Func<TransactionTotal, bool> predicate) => totals.Where(predicate).Sum(t => t.Amount);
        int Count(Func<TransactionTotal, bool> predicate) => totals.Where(predicate).Sum(t => t.Count);

        return new RevenueStatsDto
        {
            TotalRevenue = Sum(t => t.Method == PaymentMethod.PayOS),
            CourseRevenue = Sum(t => t.Type == TransactionType.CoursePurchase),
            ProRevenue = Sum(t => t.Type == TransactionType.ProUpgrade),
            DepositRevenue = Sum(t => t.Type == TransactionType.Deposit),
            CoursesSold = Count(t => t.Type == TransactionType.CoursePurchase),
            ProUpgradesSold = Count(t => t.Type == TransactionType.ProUpgrade),
            OutstandingWalletBalance = await _userRepository.GetTotalWalletBalanceAsync(),
            RecentTransactions = recent.Select(MapToResponse).ToList()
        };
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    internal decimal ProPrice(ProPackage package)
        => package == ProPackage.Year ? _settings.ProYearPrice : _settings.ProMonthPrice;

    private async Task<Course> GetPurchasableCourseAsync(int userId, int? courseId)
    {
        if (courseId is not int id)
            throw new BusinessRuleException("CourseId is required for a course checkout.");

        var course = await _courseRepository.GetByIdAsync(id);
        if (course == null || !course.IsPublished)
            throw new NotFoundException("Course", id);

        if (course.Price <= 0)
            throw new BusinessRuleException("This course is free. Enroll directly instead.");

        if (await _enrollmentRepository.IsEnrolledAsync(userId, id))
            throw new ConflictException("You are already enrolled in this course.");

        return course;
    }

    private async Task<User> GetUserAsync(int userId)
        => await _userRepository.GetByIdAsync(userId) ?? throw new NotFoundException("User", userId);

    private async Task<PagedResult<TransactionDto>> SearchAsync(int? userId, TransactionQueryParams query)
    {
        var (sortBy, sortDesc) = query.ParseSort();
        var (items, total) = await _transactionRepository.SearchAsync(
            userId, query.Type, query.Status, query.PageNumber, query.PageSize, sortBy, sortDesc);

        return PagedResult<TransactionDto>.Create(items.Select(MapToResponse), total, query);
    }

    /// <summary>
    /// Client-supplied return/cancel URLs must point at the configured frontend origin; otherwise
    /// the checkout could be used to redirect buyers to an arbitrary site.
    /// </summary>
    private string ResolveFrontendUrl(string? requested, string defaultPath)
    {
        var frontend = new Uri(_settings.FrontendBaseUrl);
        if (string.IsNullOrWhiteSpace(requested))
            return new Uri(frontend, defaultPath).ToString();

        if (!Uri.TryCreate(requested, UriKind.Absolute, out var url)
            || Uri.Compare(url, frontend, UriComponents.SchemeAndServer, UriFormat.Unescaped, StringComparison.OrdinalIgnoreCase) != 0)
            throw new BusinessRuleException($"Return and cancel URLs must be on {frontend.GetLeftPart(UriPartial.Authority)}.");

        return url.ToString();
    }

    private static string Truncate(string value, int maxLength)
        => value.Length <= maxLength ? value : value[..maxLength];

    internal static TransactionDto MapToResponse(Transaction t) => new()
    {
        TransactionId = t.TransactionId,
        OrderCode = t.OrderCode,
        UserId = t.UserId,
        UserFullName = t.User?.FullName ?? string.Empty,
        Type = t.Type.ToString(),
        Method = t.Method.ToString(),
        Status = t.Status.ToString(),
        CourseId = t.CourseId,
        CourseTitle = t.Course?.Title,
        ProPackage = t.ProPackage?.ToString(),
        Amount = t.Amount,
        Currency = t.Currency,
        CreatedAt = t.CreatedAt,
        PaidAt = t.PaidAt
    };
}
