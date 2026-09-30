using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OverLoad.Repositories.Migrations
{
    /// <summary>
    /// Replaces the "fake course" payment model with typed transactions, a wallet balance and a PRO
    /// expiry on Users, and converts the existing data:
    /// <list type="bullet">
    /// <item>type / method / pro_package are derived from the system course each transaction pointed at
    /// (system-deposit-balance, pro-upgrade-month, pro-upgrade-year) and from negative amounts (wallet spending);</item>
    /// <item>amounts become positive, statuses become PascalCase (PENDING -> Pending ...);</item>
    /// <item>payment_time is split into created_at and paid_at;</item>
    /// <item>Users.WalletBalance = successful deposits - successful wallet spending;</item>
    /// <item>Users.ProExpiresAt = latest successful PRO purchase + its period;</item>
    /// <item>enrollments into the system courses and the system courses themselves are deleted.</item>
    /// </list>
    /// BACK UP THE DATABASE BEFORE APPLYING. The data conversion cannot be reversed; Down() refuses to run.
    /// </summary>
    public partial class RefactorPaymentsAndWallet : Migration
    {
        private const string SystemCourseSlugs = "'system-deposit-balance', 'pro-upgrade-month', 'pro-upgrade-year'";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── 1. New columns (old ones are dropped only after the data is copied) ──────────
            migrationBuilder.AddColumn<DateTime>(
                name: "ProExpiresAt",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WalletBalance",
                table: "Users",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<int>(
                name: "course_id",
                table: "transactions",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "transactions",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            migrationBuilder.AddColumn<string>(
                name: "method",
                table: "transactions",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "PayOS");

            migrationBuilder.AddColumn<DateTime>(
                name: "paid_at",
                table: "transactions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pro_package",
                table: "transactions",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "type",
                table: "transactions",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "CoursePurchase");

            // ── 2. Convert existing transactions ────────────────────────────────────────────
            // payment_time held the creation time, overwritten with the payment time on success.
            migrationBuilder.Sql(@"
UPDATE transactions SET created_at = payment_time;
UPDATE transactions SET paid_at = payment_time WHERE status = 'SUCCESS';");

            // Method must be read from the sign before amounts are made positive.
            migrationBuilder.Sql(@"
UPDATE t SET
    type = CASE
        WHEN c.Slug = 'system-deposit-balance' THEN 'Deposit'
        WHEN c.Slug IN ('pro-upgrade-month', 'pro-upgrade-year') THEN 'ProUpgrade'
        ELSE 'CoursePurchase' END,
    pro_package = CASE c.Slug
        WHEN 'pro-upgrade-month' THEN 'Month'
        WHEN 'pro-upgrade-year' THEN 'Year' END,
    method = CASE WHEN t.amount < 0 THEN 'Wallet' ELSE 'PayOS' END
FROM transactions t
JOIN Courses c ON c.Id = t.course_id;

UPDATE transactions SET amount = -amount WHERE amount < 0;

UPDATE transactions SET status = CASE status
    WHEN 'PENDING' THEN 'Pending'
    WHEN 'SUCCESS' THEN 'Success'
    ELSE 'Cancelled' END;");

            // ── 3. Wallet balances and PRO expiry ───────────────────────────────────────────
            migrationBuilder.Sql(@"
UPDATE u SET WalletBalance = x.balance
FROM Users u
JOIN (
    SELECT user_id,
           SUM(CASE WHEN type = 'Deposit' THEN amount
                    WHEN method = 'Wallet' THEN -amount
                    ELSE 0 END) AS balance
    FROM transactions
    WHERE status = 'Success'
    GROUP BY user_id
) x ON x.user_id = u.Id;

UPDATE u SET ProExpiresAt = x.expires_at
FROM Users u
JOIN (
    SELECT user_id,
           MAX(CASE pro_package WHEN 'Year' THEN DATEADD(year, 1, paid_at)
                                ELSE DATEADD(month, 1, paid_at) END) AS expires_at
    FROM transactions
    WHERE status = 'Success' AND type = 'ProUpgrade'
    GROUP BY user_id
) x ON x.user_id = u.Id;");

            // ── 4. Remove the system courses ────────────────────────────────────────────────
            migrationBuilder.Sql($@"
UPDATE t SET course_id = NULL
FROM transactions t
JOIN Courses c ON c.Id = t.course_id
WHERE c.Slug IN ({SystemCourseSlugs});

DELETE e
FROM Enrollments e
JOIN Courses c ON c.Id = e.CourseId
WHERE c.Slug IN ({SystemCourseSlugs});

DELETE FROM Courses WHERE Slug IN ({SystemCourseSlugs});");

            // ── 5. Drop the old column, add indexes ─────────────────────────────────────────
            migrationBuilder.DropColumn(
                name: "payment_time",
                table: "transactions");

            migrationBuilder.DropIndex(
                name: "IX_transactions_user_id",
                table: "transactions");

            // Fails (and the migration rolls back) if two old orders share an order code.
            migrationBuilder.CreateIndex(
                name: "IX_transactions_order_code",
                table: "transactions",
                column: "order_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_transactions_user_id_status",
                table: "transactions",
                columns: new[] { "user_id", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The system courses and their enrollments are deleted by Up(), and wallet balances /
            // PRO expiry are derived data; the old shape cannot be rebuilt reliably.
            throw new NotSupportedException(
                "RefactorPaymentsAndWallet converts payment data and cannot be reverted. Restore the database backup taken before applying it.");
        }
    }
}
