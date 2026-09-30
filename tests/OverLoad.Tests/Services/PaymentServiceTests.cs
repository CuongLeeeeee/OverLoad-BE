using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OverLoad.Domain.Enums;
using OverLoad.Repositories.Data;
using OverLoad.Repositories.Implementations;
using OverLoad.Services.DTOs.Request;
using OverLoad.Services.Exceptions;
using OverLoad.Services.Implementations;
using OverLoad.Services.Interfaces;
using OverLoad.Services.Settings;
using Xunit;

namespace OverLoad.Tests.Services;

public class PaymentServiceTests
{
    internal static readonly PaymentSettings Settings = new()
    {
        FrontendBaseUrl = "https://app.overload.test",
        ProMonthPrice = 69000,
        ProYearPrice = 599000,
        MinDepositAmount = 10000,
        MaxDepositAmount = 5000000
    };

    internal static PaymentService CreateService(AppDbContext context, FakePaymentGateway gateway) => new(
        new TransactionRepository(context),
        new UserRepository(context),
        new CourseRepository(context),
        new EnrollmentRepository(context),
        new UnitOfWork(context),
        gateway,
        Options.Create(Settings),
        NullLogger<PaymentService>.Instance);

    // ── Checkout ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task CheckoutAsync_Course_Should_Create_Pending_Transaction_With_Course_Price()
    {
        await using var context = TestData.CreateContext();
        var data = await TestData.SeedAsync(context);
        var gateway = new FakePaymentGateway();

        var result = await CreateService(context, gateway).CheckoutAsync(
            data.Student.Id, new CheckoutRequest { Type = CheckoutType.Course, CourseId = data.PaidCourse.Id });

        var transaction = await context.Transactions.SingleAsync();
        Assert.Equal(TransactionType.CoursePurchase, transaction.Type);
        Assert.Equal(TransactionStatus.Pending, transaction.Status);
        Assert.Equal(PaymentMethod.PayOS, transaction.Method);
        Assert.Equal(data.PaidCourse.Price, transaction.Amount);
        Assert.Equal($"plink-{result.OrderCode}", transaction.TransactionId);
        Assert.Equal("https://app.overload.test/payment/success", gateway.CreatedLinks.Single().ReturnUrl);
    }

    [Fact]
    public async Task CheckoutAsync_Should_Reject_Free_Draft_And_Owned_Courses()
    {
        await using var context = TestData.CreateContext();
        var data = await TestData.SeedAsync(context);
        await TestData.EnrollAsync(context, data.Student, data.PaidCourse);
        var service = CreateService(context, new FakePaymentGateway());

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.CheckoutAsync(
            data.Student.Id, new CheckoutRequest { Type = CheckoutType.Course, CourseId = data.FreeCourse.Id }));
        await Assert.ThrowsAsync<NotFoundException>(() => service.CheckoutAsync(
            data.Student.Id, new CheckoutRequest { Type = CheckoutType.Course, CourseId = data.DraftCourse.Id }));
        await Assert.ThrowsAsync<ConflictException>(() => service.CheckoutAsync(
            data.Student.Id, new CheckoutRequest { Type = CheckoutType.Course, CourseId = data.PaidCourse.Id }));
        Assert.Empty(context.Transactions);
    }

    [Theory]
    [InlineData(ProPackage.Month, 69000)]
    [InlineData(ProPackage.Year, 599000)]
    public async Task CheckoutAsync_Pro_Should_Use_Configured_Price(ProPackage package, decimal expectedPrice)
    {
        await using var context = TestData.CreateContext();
        var data = await TestData.SeedAsync(context);

        var result = await CreateService(context, new FakePaymentGateway()).CheckoutAsync(
            data.Student.Id, new CheckoutRequest { Type = CheckoutType.Pro, Package = package });

        Assert.Equal(expectedPrice, result.Amount);
        Assert.Equal(package, (await context.Transactions.SingleAsync()).ProPackage);
    }

    [Theory]
    [InlineData(5000)]        // below minimum
    [InlineData(9999999)]     // above maximum
    [InlineData(10000.5)]     // not a whole amount
    public async Task CheckoutAsync_Deposit_Should_Enforce_Limits(decimal amount)
    {
        await using var context = TestData.CreateContext();
        var data = await TestData.SeedAsync(context);

        await Assert.ThrowsAsync<BusinessRuleException>(() => CreateService(context, new FakePaymentGateway()).CheckoutAsync(
            data.Student.Id, new CheckoutRequest { Type = CheckoutType.Deposit, Amount = amount }));
    }

    [Theory]
    [InlineData("https://evil.example/payment/success")]
    [InlineData("https://app.overload.test.evil.example/x")]
    [InlineData("not a url")]
    public async Task CheckoutAsync_Should_Reject_Return_Url_Outside_Frontend(string returnUrl)
    {
        await using var context = TestData.CreateContext();
        var data = await TestData.SeedAsync(context);

        await Assert.ThrowsAsync<BusinessRuleException>(() => CreateService(context, new FakePaymentGateway()).CheckoutAsync(
            data.Student.Id, new CheckoutRequest { Type = CheckoutType.Deposit, Amount = 50000, ReturnUrl = returnUrl }));
    }

    [Fact]
    public async Task CheckoutAsync_Should_Not_Save_Anything_When_Gateway_Fails()
    {
        await using var context = TestData.CreateContext();
        var data = await TestData.SeedAsync(context);

        await Assert.ThrowsAsync<ExternalServiceException>(() => CreateService(context, new FakePaymentGateway { Unreachable = true })
            .CheckoutAsync(data.Student.Id, new CheckoutRequest { Type = CheckoutType.Deposit, Amount = 50000 }));
        Assert.Empty(context.Transactions);
    }

    // ── Fulfilment ───────────────────────────────────────────────────────────

    [Fact]
    public async Task ConfirmPaidAsync_Course_Should_Enroll_Once_Even_When_Called_Twice()
    {
        await using var context = TestData.CreateContext();
        var data = await TestData.SeedAsync(context);
        var service = CreateService(context, new FakePaymentGateway());
        var order = await service.CheckoutAsync(data.Student.Id, new CheckoutRequest { Type = CheckoutType.Course, CourseId = data.PaidCourse.Id });

        Assert.True(await service.ConfirmPaidAsync(order.OrderCode, (long)order.Amount));
        Assert.True(await service.ConfirmPaidAsync(order.OrderCode, (long)order.Amount));   // webhook retry

        var transaction = await context.Transactions.SingleAsync();
        Assert.Equal(TransactionStatus.Success, transaction.Status);
        Assert.NotNull(transaction.PaidAt);
        Assert.Single(context.Enrollments.Where(e => e.UserId == data.Student.Id && e.CourseId == data.PaidCourse.Id));
    }

    [Fact]
    public async Task ConfirmPaidAsync_Deposit_Should_Credit_Wallet_Once()
    {
        await using var context = TestData.CreateContext();
        var data = await TestData.SeedAsync(context);
        var service = CreateService(context, new FakePaymentGateway());
        var order = await service.CheckoutAsync(data.Student.Id, new CheckoutRequest { Type = CheckoutType.Deposit, Amount = 100000 });

        await service.ConfirmPaidAsync(order.OrderCode, 100000);
        await service.ConfirmPaidAsync(order.OrderCode, 100000);

        Assert.Equal(100000m, (await context.Users.FindAsync(data.Student.Id))!.WalletBalance);
        Assert.Empty(context.Enrollments);   // deposits no longer enroll into a fake course
    }

    [Fact]
    public async Task ConfirmPaidAsync_Pro_Should_Stack_On_Active_Subscription()
    {
        await using var context = TestData.CreateContext();
        var data = await TestData.SeedAsync(context);
        var currentExpiry = DateTime.UtcNow.AddDays(10);
        await TestData.MakeProAsync(context, data.Student, currentExpiry);
        var service = CreateService(context, new FakePaymentGateway());
        var order = await service.CheckoutAsync(data.Student.Id, new CheckoutRequest { Type = CheckoutType.Pro, Package = ProPackage.Month });

        await service.ConfirmPaidAsync(order.OrderCode, (long)order.Amount);

        Assert.Equal(currentExpiry.AddMonths(1), (await context.Users.FindAsync(data.Student.Id))!.ProExpiresAt);
    }

    [Fact]
    public async Task ConfirmPaidAsync_Should_Leave_Underpaid_Order_Pending()
    {
        await using var context = TestData.CreateContext();
        var data = await TestData.SeedAsync(context);
        var service = CreateService(context, new FakePaymentGateway());
        var order = await service.CheckoutAsync(data.Student.Id, new CheckoutRequest { Type = CheckoutType.Course, CourseId = data.PaidCourse.Id });

        await service.ConfirmPaidAsync(order.OrderCode, (long)order.Amount - 1);

        Assert.Equal(TransactionStatus.Pending, (await context.Transactions.SingleAsync()).Status);
        Assert.Empty(context.Enrollments);
    }

    [Fact]
    public async Task ConfirmPaidAsync_Should_Return_False_For_Unknown_Order()
    {
        await using var context = TestData.CreateContext();

        Assert.False(await CreateService(context, new FakePaymentGateway()).ConfirmPaidAsync(123, 1000));
    }

    // ── Sync with the gateway ────────────────────────────────────────────────

    [Fact]
    public async Task SyncPendingAsync_Should_Fulfil_Paid_And_Cancel_Closed_Orders()
    {
        await using var context = TestData.CreateContext();
        var data = await TestData.SeedAsync(context);
        var gateway = new FakePaymentGateway();
        var service = CreateService(context, gateway);
        var paid = await service.CheckoutAsync(data.Student.Id, new CheckoutRequest { Type = CheckoutType.Deposit, Amount = 20000 });
        var closed = await service.CheckoutAsync(data.Student.Id, new CheckoutRequest { Type = CheckoutType.Deposit, Amount = 30000 });
        var pending = await service.CheckoutAsync(data.Student.Id, new CheckoutRequest { Type = CheckoutType.Deposit, Amount = 40000 });
        gateway.Statuses[paid.OrderCode] = new GatewayPaymentStatus(GatewayPaymentState.Paid, 20000);
        gateway.Statuses[closed.OrderCode] = new GatewayPaymentStatus(GatewayPaymentState.Closed, 0);

        await service.SyncPendingAsync(data.Student.Id);

        var statuses = await context.Transactions.ToDictionaryAsync(t => t.OrderCode, t => t.Status);
        Assert.Equal(TransactionStatus.Success, statuses[paid.OrderCode]);
        Assert.Equal(TransactionStatus.Cancelled, statuses[closed.OrderCode]);
        Assert.Equal(TransactionStatus.Pending, statuses[pending.OrderCode]);
        Assert.Equal(20000m, (await context.Users.FindAsync(data.Student.Id))!.WalletBalance);
    }

    [Fact]
    public async Task GetMyOrderAsync_Should_Hide_Other_Users_Orders()
    {
        await using var context = TestData.CreateContext();
        var data = await TestData.SeedAsync(context);
        var service = CreateService(context, new FakePaymentGateway());
        var order = await service.CheckoutAsync(data.Student.Id, new CheckoutRequest { Type = CheckoutType.Deposit, Amount = 20000 });

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetMyOrderAsync(data.Instructor.Id, order.OrderCode));
    }

    // ── Reporting ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetRevenueStatsAsync_Should_Count_Only_Money_Received_Through_PayOS()
    {
        await using var context = TestData.CreateContext();
        var data = await TestData.SeedAsync(context);
        var service = CreateService(context, new FakePaymentGateway());
        var course = await service.CheckoutAsync(data.Student.Id, new CheckoutRequest { Type = CheckoutType.Course, CourseId = data.PaidCourse.Id });
        var deposit = await service.CheckoutAsync(data.Student.Id, new CheckoutRequest { Type = CheckoutType.Deposit, Amount = 100000 });
        await service.CheckoutAsync(data.Student.Id, new CheckoutRequest { Type = CheckoutType.Deposit, Amount = 70000 });   // never paid
        await service.ConfirmPaidAsync(course.OrderCode, (long)course.Amount);
        await service.ConfirmPaidAsync(deposit.OrderCode, 100000);
        await WalletServiceTests.CreateService(context, service).PurchaseProAsync(data.Student.Id, ProPackage.Month);

        var stats = await service.GetRevenueStatsAsync();

        Assert.Equal(data.PaidCourse.Price + 100000, stats.TotalRevenue);   // wallet spend is not new revenue
        Assert.Equal(1, stats.CoursesSold);
        Assert.Equal(1, stats.ProUpgradesSold);
        Assert.Equal(100000m - 69000m, stats.OutstandingWalletBalance);
        Assert.Equal(3, stats.RecentTransactions.Count);
    }
}
