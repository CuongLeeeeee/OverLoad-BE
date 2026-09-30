namespace OverLoad.DataTransfer;

/// <summary>One database row: column name → value (null for SQL NULL). Column names are case-insensitive.</summary>
public sealed class Row : Dictionary<string, object?>
{
    public Row() : base(StringComparer.OrdinalIgnoreCase) { }

    public Row(IDictionary<string, object?> values) : base(values, StringComparer.OrdinalIgnoreCase) { }
}

/// <summary>Rows per table, keyed by table name.</summary>
public sealed class Snapshot : Dictionary<string, List<Row>>
{
    public Snapshot() : base(StringComparer.OrdinalIgnoreCase) { }

    public List<Row> Rows(string table) => TryGetValue(table, out var rows) ? rows : new List<Row>();
}

/// <summary>
/// Converts the old SQL Server data to the new model. This is the logic of the SQL Server migration
/// RefactorPaymentsAndWallet (verified on LocalDB), ported to C# because the PostgreSQL schema is
/// created fresh instead of being migrated:
/// <list type="bullet">
/// <item>transaction type / method / PRO package come from the "system" course a transaction pointed at
/// and from negative amounts (wallet spending); amounts become positive; statuses become PascalCase;</item>
/// <item>payment_time is split into created_at and paid_at (successful payments only);</item>
/// <item>Users.WalletBalance = successful deposits − successful wallet spending;
/// Users.ProExpiresAt = latest successful PRO purchase + its period;</item>
/// <item>the system courses and enrollments into them are dropped;</item>
/// <item>every DateTime is marked UTC (the old code always stored UTC).</item>
/// </list>
/// </summary>
public static class LegacyTransform
{
    public const string DepositSlug = "system-deposit-balance";
    public const string ProMonthSlug = "pro-upgrade-month";
    public const string ProYearSlug = "pro-upgrade-year";

    /// <summary>Tables in foreign-key order: parents before children.</summary>
    public static readonly string[] TableOrder =
        { "Users", "Courses", "Lessons", "Enrollments", "UserLessonProgresses", "transactions", "BugReports" };

    private static readonly HashSet<string> SystemSlugs =
        new(StringComparer.OrdinalIgnoreCase) { DepositSlug, ProMonthSlug, ProYearSlug };

    public sealed record Result(Snapshot Data, List<string> Notes);

    public static Result Transform(Snapshot source)
    {
        var notes = new List<string>();
        var data = new Snapshot();
        foreach (var table in TableOrder)
            data[table] = source.Rows(table).Select(r => new Row(r)).ToList();

        foreach (var row in data.Values.SelectMany(rows => rows))
            MarkDatesUtc(row);

        // ── System courses ──────────────────────────────────────────────────
        var systemCourses = data["Courses"]
            .Where(c => SystemSlugs.Contains((string)c["Slug"]!))
            .ToDictionary(c => Convert.ToInt32(c["Id"]), c => (string)c["Slug"]!);

        data["Courses"] = data["Courses"].Where(c => !systemCourses.ContainsKey(Convert.ToInt32(c["Id"]))).ToList();

        var enrollmentsBefore = data["Enrollments"].Count;
        data["Enrollments"] = data["Enrollments"]
            .Where(e => !systemCourses.ContainsKey(Convert.ToInt32(e["CourseId"])))
            .ToList();
        notes.Add($"Dropped {systemCourses.Count} system course(s) and {enrollmentsBefore - data["Enrollments"].Count} enrollment(s) into them.");

        if (data["Lessons"].Any(l => systemCourses.ContainsKey(Convert.ToInt32(l["CourseId"]))))
            throw new InvalidOperationException("A lesson belongs to a system course; resolve it before transferring.");
        if (data["BugReports"].Any(b => systemCourses.ContainsKey(Convert.ToInt32(b["CourseId"]))))
            throw new InvalidOperationException("A bug report points at a system course; resolve it before transferring.");

        // ── Transactions ────────────────────────────────────────────────────
        foreach (var t in data["transactions"])
        {
            var courseId = Convert.ToInt32(t["course_id"]);
            systemCourses.TryGetValue(courseId, out var slug);

            var amount = Convert.ToDecimal(t["amount"]);
            var status = MapStatus((string?)t["status"], notes, Convert.ToInt64(t["order_code"]));
            var paymentTime = (DateTime)t["payment_time"]!;

            t["type"] = slug switch
            {
                DepositSlug => "Deposit",
                ProMonthSlug or ProYearSlug => "ProUpgrade",
                _ => "CoursePurchase"
            };
            t["pro_package"] = slug switch { ProMonthSlug => "Month", ProYearSlug => "Year", _ => null };
            t["method"] = amount < 0 ? "Wallet" : "PayOS";
            t["amount"] = Math.Abs(amount);
            t["status"] = status;
            t["course_id"] = slug == null ? courseId : null;
            t["created_at"] = paymentTime;
            t["paid_at"] = status == "Success" ? paymentTime : null;
            t.Remove("payment_time");
        }

        // ── Wallet balances and PRO expiry ──────────────────────────────────
        var successful = data["transactions"].Where(t => (string)t["status"]! == "Success").ToList();
        foreach (var user in data["Users"])
        {
            var userId = Convert.ToInt32(user["Id"]);
            var own = successful.Where(t => Convert.ToInt32(t["user_id"]) == userId).ToList();

            user["WalletBalance"] = own.Sum(t =>
                (string)t["type"]! == "Deposit" ? (decimal)t["amount"]!
                : (string)t["method"]! == "Wallet" ? -(decimal)t["amount"]!
                : 0m);

            user["ProExpiresAt"] = own
                .Where(t => (string)t["type"]! == "ProUpgrade")
                .Select(t => (string?)t["pro_package"] == "Year"
                    ? ((DateTime)t["paid_at"]!).AddYears(1)
                    : ((DateTime)t["paid_at"]!).AddMonths(1))
                .Select(d => (DateTime?)d)
                .DefaultIfEmpty(null)
                .Max();
        }

        var negative = data["Users"].Where(u => (decimal)u["WalletBalance"]! < 0).ToList();
        if (negative.Count > 0)
            notes.Add($"WARNING: {negative.Count} user(s) end with a negative wallet balance: ids {string.Join(", ", negative.Select(u => u["Id"]))}.");

        return new Result(data, notes);
    }

    private static string MapStatus(string? status, List<string> notes, long orderCode)
    {
        switch (status?.Trim().ToUpperInvariant())
        {
            case "PENDING": return "Pending";
            case "SUCCESS": return "Success";
            case "CANCELLED":
            case "CANCELED": return "Cancelled";
            default:
                notes.Add($"WARNING: order {orderCode} has unknown status '{status}', stored as Cancelled.");
                return "Cancelled";
        }
    }

    private static void MarkDatesUtc(Row row)
    {
        foreach (var key in row.Keys.ToList())
        {
            if (row[key] is DateTime d && d.Kind != DateTimeKind.Utc)
                row[key] = DateTime.SpecifyKind(d, DateTimeKind.Utc);
        }
    }
}
