using OverLoad.DataTransfer;
using Xunit;

namespace OverLoad.Tests.DataTransfer;

/// <summary>
/// Same scenario that was used to verify the SQL Server migration RefactorPaymentsAndWallet on LocalDB,
/// so the C# port must produce the same results.
/// </summary>
public class LegacyTransformTests
{
    private static readonly DateTime T0 = new(2026, 9, 1, 10, 0, 0, DateTimeKind.Unspecified);

    private static Snapshot OldData()
    {
        Row User(int id, string email) => new() { ["Id"] = id, ["Email"] = email, ["Role"] = "Student", ["CreatedAt"] = T0 };
        Row Course(int id, string slug, decimal price) => new() { ["Id"] = id, ["Slug"] = slug, ["Price"] = price, ["CreatedAt"] = T0 };
        Row Enroll(int id, int user, int course) => new() { ["Id"] = id, ["UserId"] = user, ["CourseId"] = course, ["EnrolledAt"] = T0 };
        Row Tx(long order, int user, int course, decimal amount, string status, DateTime at) => new()
        {
            ["transaction_id"] = $"pl-{order}", ["order_code"] = order, ["user_id"] = user, ["course_id"] = course,
            ["amount"] = amount, ["currency"] = "VND", ["status"] = status, ["payment_time"] = at
        };

        return new Snapshot
        {
            ["Users"] = new() { User(1, "alice"), User(2, "bob"), User(3, "carol") },
            ["Courses"] = new()
            {
                Course(1, "real-course", 199000), Course(2, LegacyTransform.DepositSlug, 100000),
                Course(3, LegacyTransform.ProMonthSlug, 69000), Course(4, LegacyTransform.ProYearSlug, 599000)
            },
            ["Lessons"] = new() { new Row { ["Id"] = 1, ["CourseId"] = 1, ["CreatedAt"] = T0 } },
            ["Enrollments"] = new() { Enroll(1, 1, 1), Enroll(2, 1, 2), Enroll(3, 1, 3), Enroll(4, 2, 4) },
            ["transactions"] = new()
            {
                Tx(1001, 1, 1, 199000, "SUCCESS", T0),
                Tx(1002, 1, 2, 100000, "SUCCESS", T0.AddDays(1)),
                Tx(1003, 1, 2, 50000, "PENDING", T0.AddDays(1)),
                Tx(1004, 1, 3, -69000, "SUCCESS", T0.AddDays(2)),      // PRO month paid from the wallet
                Tx(1005, 2, 4, 599000, "SUCCESS", T0.AddDays(3)),      // PRO year through PayOS
                Tx(1006, 2, 1, 199000, "PENDING", T0.AddDays(4)),
                Tx(1007, 2, 2, 20000, "CANCELLED", T0.AddDays(5))
            }
        };
    }

    [Fact]
    public void Transform_Should_Derive_Type_Method_Package_And_Positive_Amounts()
    {
        var tx = LegacyTransform.Transform(OldData()).Data.Rows("transactions")
            .ToDictionary(t => Convert.ToInt64(t["order_code"]));

        Assert.Equal(("CoursePurchase", "PayOS", 1), ((string)tx[1001]["type"]!, (string)tx[1001]["method"]!, (int)tx[1001]["course_id"]!));
        Assert.Equal(("Deposit", "PayOS"), ((string)tx[1002]["type"]!, (string)tx[1002]["method"]!));
        Assert.Equal(("ProUpgrade", "Wallet", "Month", 69000m), ((string)tx[1004]["type"]!, (string)tx[1004]["method"]!, (string)tx[1004]["pro_package"]!, (decimal)tx[1004]["amount"]!));
        Assert.Equal(("ProUpgrade", "PayOS", "Year"), ((string)tx[1005]["type"]!, (string)tx[1005]["method"]!, (string)tx[1005]["pro_package"]!));
        Assert.Null(tx[1002]["course_id"]);   // system courses are not kept
        Assert.Null(tx[1005]["course_id"]);
    }

    [Fact]
    public void Transform_Should_Map_Statuses_And_Split_Payment_Time()
    {
        var tx = LegacyTransform.Transform(OldData()).Data.Rows("transactions")
            .ToDictionary(t => Convert.ToInt64(t["order_code"]));

        Assert.Equal("Success", tx[1001]["status"]);
        Assert.Equal("Pending", tx[1003]["status"]);
        Assert.Equal("Cancelled", tx[1007]["status"]);
        Assert.Equal(T0, tx[1001]["paid_at"]);
        Assert.Null(tx[1003]["paid_at"]);                  // not paid
        Assert.Equal(T0.AddDays(1), tx[1003]["created_at"]);
        Assert.False(tx[1001].ContainsKey("payment_time"));
    }

    [Fact]
    public void Transform_Should_Compute_Wallet_And_Pro_Like_The_Sql_Migration()
    {
        var users = LegacyTransform.Transform(OldData()).Data.Rows("Users").ToDictionary(u => (int)u["Id"]!);

        Assert.Equal(31000m, users[1]["WalletBalance"]);                   // 100 000 deposit − 69 000 wallet PRO
        Assert.Equal(T0.AddDays(2).AddMonths(1), users[1]["ProExpiresAt"]);
        Assert.Equal(0m, users[2]["WalletBalance"]);                       // cancelled deposit does not count
        Assert.Equal(T0.AddDays(3).AddYears(1), users[2]["ProExpiresAt"]);
        Assert.Null(users[3]["ProExpiresAt"]);
    }

    [Fact]
    public void Transform_Should_Drop_System_Courses_And_Their_Enrollments()
    {
        var data = LegacyTransform.Transform(OldData()).Data;

        Assert.Equal(new[] { "real-course" }, data.Rows("Courses").Select(c => (string)c["Slug"]!));
        Assert.Equal(new[] { 1 }, data.Rows("Enrollments").Select(e => (int)e["CourseId"]!));
    }

    [Fact]
    public void Transform_Should_Mark_Every_Date_As_Utc()
    {
        var data = LegacyTransform.Transform(OldData()).Data;

        var dates = data.Values.SelectMany(rows => rows).SelectMany(r => r.Values).OfType<DateTime>().ToList();
        Assert.NotEmpty(dates);
        Assert.All(dates, d => Assert.Equal(DateTimeKind.Utc, d.Kind));
    }

    [Fact]
    public void Transform_Should_Refuse_Bug_Reports_On_System_Courses()
    {
        var old = OldData();
        old["BugReports"] = new() { new Row { ["Id"] = 1, ["UserId"] = 1, ["CourseId"] = 2 } };

        Assert.Throws<InvalidOperationException>(() => LegacyTransform.Transform(old));
    }

    [Fact]
    public void Transform_Should_Not_Modify_The_Source_Snapshot()
    {
        var old = OldData();
        LegacyTransform.Transform(old);

        Assert.Equal(-69000m, old.Rows("transactions").Single(t => Convert.ToInt64(t["order_code"]) == 1004)["amount"]);
        Assert.Equal(4, old.Rows("Courses").Count);
    }
}
