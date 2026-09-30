using OverLoad.API.Models.Requests;
using OverLoad.API.Models.Responses;
using OverLoad.Services.DTOs.Request;
using OverLoad.Services.DTOs.Response;

namespace OverLoad.API.Models.Mappings;

public static class PaymentModelMapper
{
    public static CheckoutRequest ToServiceDto(this CheckoutRequestModel model) => new()
    {
        Type = model.Type,
        CourseId = model.CourseId,
        Package = model.Package,
        Amount = model.Amount,
        ReturnUrl = model.ReturnUrl,
        CancelUrl = model.CancelUrl
    };

    public static TransactionQueryParams ToServiceDto(this TransactionQueryRequestModel model) => new TransactionQueryParams
    {
        UserId = model.UserId,
        Type = model.Type,
        Status = model.Status
    }.WithPaging(model);

    public static CheckoutResponseModel ToResponseModel(this CheckoutDto dto) => new()
    {
        OrderCode = dto.OrderCode,
        Amount = dto.Amount,
        CheckoutUrl = dto.CheckoutUrl
    };

    public static TransactionResponseModel ToResponseModel(this TransactionDto dto) => new()
    {
        TransactionId = dto.TransactionId,
        OrderCode = dto.OrderCode,
        UserId = dto.UserId,
        UserFullName = dto.UserFullName,
        Type = dto.Type,
        Method = dto.Method,
        Status = dto.Status,
        CourseId = dto.CourseId,
        CourseTitle = dto.CourseTitle,
        ProPackage = dto.ProPackage,
        Amount = dto.Amount,
        Currency = dto.Currency,
        CreatedAt = dto.CreatedAt,
        PaidAt = dto.PaidAt
    };

    public static WalletResponseModel ToResponseModel(this WalletDto dto) => new()
    {
        Balance = dto.Balance,
        IsPro = dto.IsPro,
        ProExpiresAt = dto.ProExpiresAt
    };

    public static RevenueStatsResponseModel ToResponseModel(this RevenueStatsDto dto) => new()
    {
        TotalRevenue = dto.TotalRevenue,
        CourseRevenue = dto.CourseRevenue,
        ProRevenue = dto.ProRevenue,
        DepositRevenue = dto.DepositRevenue,
        CoursesSold = dto.CoursesSold,
        ProUpgradesSold = dto.ProUpgradesSold,
        OutstandingWalletBalance = dto.OutstandingWalletBalance,
        RecentTransactions = dto.RecentTransactions.Select(t => t.ToResponseModel()).ToList()
    };
}
