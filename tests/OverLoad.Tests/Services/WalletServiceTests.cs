using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OverLoad.Domain.Enums;
using OverLoad.Repositories.Data;
using OverLoad.Repositories.Implementations;
using OverLoad.Services.Exceptions;
using OverLoad.Services.Implementations;
using OverLoad.Services.Interfaces;
using Xunit;

namespace OverLoad.Tests.Services;

public class WalletServiceTests
{
    internal static WalletService CreateService(AppDbContext context, IPaymentService? paymentService = null) => new(
        new UserRepository(context),
        new TransactionRepository(context),
        new UnitOfWork(context),
        paymentService ?? PaymentServiceTests.CreateService(context, new FakePaymentGateway()),
        Options.Create(PaymentServiceTests.Settings));

    private static async Task SetBalanceAsync(AppDbContext context, int userId, decimal balance)
    {
        (await context.Users.FindAsync(userId))!.WalletBalance = balance;
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task PurchaseProAsync_Should_Debit_Wallet_Extend_Pro_And_Record_Transaction()
    {
        await using var context = TestData.CreateContext();
        var data = await TestData.SeedAsync(context);
        await SetBalanceAsync(context, data.Student.Id, 100000);

        var wallet = await CreateService(context).PurchaseProAsync(data.Student.Id, ProPackage.Month);

        Assert.Equal(31000m, wallet.Balance);
        Assert.True(wallet.IsPro);
        var transaction = await context.Transactions.SingleAsync();
        Assert.Equal(TransactionType.ProUpgrade, transaction.Type);
        Assert.Equal(PaymentMethod.Wallet, transaction.Method);
        Assert.Equal(TransactionStatus.Success, transaction.Status);
        Assert.Equal(69000m, transaction.Amount);   // stored positive; Method says it came from the wallet
    }

    [Fact]
    public async Task PurchaseProAsync_Should_Reject_Insufficient_Balance_Without_Changes()
    {
        await using var context = TestData.CreateContext();
        var data = await TestData.SeedAsync(context);
        await SetBalanceAsync(context, data.Student.Id, 68999);

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => CreateService(context).PurchaseProAsync(data.Student.Id, ProPackage.Month));

        context.ChangeTracker.Clear();
        var user = await context.Users.SingleAsync(u => u.Id == data.Student.Id);
        Assert.Equal(68999m, user.WalletBalance);
        Assert.Null(user.ProExpiresAt);
        Assert.Empty(context.Transactions);
    }

    [Fact]
    public async Task PurchaseProAsync_Should_Credit_Pending_Deposit_That_Was_Paid_Before_Checking_Balance()
    {
        await using var context = TestData.CreateContext();
        var data = await TestData.SeedAsync(context);
        var gateway = new FakePaymentGateway();
        var payments = PaymentServiceTests.CreateService(context, gateway);
        var deposit = await payments.CheckoutAsync(data.Student.Id,
            new() { Type = OverLoad.Services.DTOs.Request.CheckoutType.Deposit, Amount = 70000 });
        gateway.Statuses[deposit.OrderCode] = new GatewayPaymentStatus(GatewayPaymentState.Paid, 70000);   // webhook not received

        var wallet = await CreateService(context, payments).PurchaseProAsync(data.Student.Id, ProPackage.Month);

        Assert.Equal(1000m, wallet.Balance);
    }

    [Fact]
    public async Task WalletBalance_Is_A_Concurrency_Token_So_A_Stale_Debit_Fails()
    {
        // Two requests read the same balance; the second write must not overwrite the first.
        var databaseName = Guid.NewGuid().ToString();
        int userId;
        await using (var seed = TestData.CreateContext(databaseName))
        {
            var data = await TestData.SeedAsync(seed);
            userId = data.Student.Id;
            await SetBalanceAsync(seed, userId, 100000);
        }

        await using var first = TestData.CreateContext(databaseName);
        await using var second = TestData.CreateContext(databaseName);
        var firstUser = await first.Users.SingleAsync(u => u.Id == userId);
        var secondUser = await second.Users.SingleAsync(u => u.Id == userId);

        firstUser.WalletBalance -= 69000;
        await first.SaveChangesAsync();

        secondUser.WalletBalance -= 69000;
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());
    }

    [Fact]
    public async Task Pro_User_Should_Access_Paid_Lessons_And_Self_Enroll_Without_Buying()
    {
        await using var context = TestData.CreateContext();
        var data = await TestData.SeedAsync(context);
        await TestData.MakeProAsync(context, data.Student);
        var lessons = new LessonService(new LessonRepository(context), new CourseRepository(context),
            new UserLessonProgressRepository(context), TestData.CourseAccess(context));
        var enrollments = new EnrollmentService(new EnrollmentRepository(context), new UserRepository(context),
            new CourseRepository(context), new UserLessonProgressRepository(context), TestData.CourseAccess(context));

        var lesson = await lessons.GetByIdAsync(data.PaidCourseLessons[2].Id, data.Student.Id, isStaff: false);
        var enrollment = await enrollments.SelfEnrollAsync(data.Student.Id, data.PaidCourse.Id);

        Assert.Equal("secret-3", lesson.Content);
        Assert.Equal(data.PaidCourse.Id, enrollment.CourseId);
    }

    [Fact]
    public async Task Expired_Pro_Should_Not_Grant_Access()
    {
        await using var context = TestData.CreateContext();
        var data = await TestData.SeedAsync(context);
        await TestData.MakeProAsync(context, data.Student, DateTime.UtcNow.AddMinutes(-1));
        var lessons = new LessonService(new LessonRepository(context), new CourseRepository(context),
            new UserLessonProgressRepository(context), TestData.CourseAccess(context));

        await Assert.ThrowsAsync<ForbiddenException>(
            () => lessons.GetByIdAsync(data.PaidCourseLessons[2].Id, data.Student.Id, isStaff: false));
    }
}
