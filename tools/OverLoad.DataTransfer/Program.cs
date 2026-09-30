using System.Data;
using Microsoft.Data.SqlClient;
using Npgsql;
using OverLoad.DataTransfer;
using OverLoad.Repositories.Data;

// One-off copy of the old SQL Server (Azure) database into the PostgreSQL schema created by
// InitialPostgres, converting payment data on the way (see LegacyTransform).
//
//   dotnet run --project tools/OverLoad.DataTransfer            dry run: writes, checks, then ROLLS BACK
//   dotnet run --project tools/OverLoad.DataTransfer -- --apply  commits
//
// Source: SOURCE_SQLSERVER_CONNECTION (read only — SELECTs only).
// Target: DATABASE_URL (postgresql://...). Both can live in the repo's .env.

var apply = args.Contains("--apply");
DatabaseConnection.LoadDotEnv();

var sourceConnection = Environment.GetEnvironmentVariable("SOURCE_SQLSERVER_CONNECTION");
var targetUrl = Environment.GetEnvironmentVariable(DatabaseConnection.UrlVariable);
if (string.IsNullOrWhiteSpace(sourceConnection) || string.IsNullOrWhiteSpace(targetUrl))
{
    Console.Error.WriteLine("Set SOURCE_SQLSERVER_CONNECTION and DATABASE_URL (e.g. in .env).");
    return 2;
}

var targetConnection = DatabaseConnection.FromUrl(targetUrl);
Console.WriteLine($"Source: {DescribeSqlServer(sourceConnection)}");
Console.WriteLine($"Target: {DescribePostgres(targetConnection)}");
Console.WriteLine($"Mode:   {(apply ? "APPLY (commit)" : "DRY RUN (rollback)")}");
Console.WriteLine();

// ── 1. Read the source ──────────────────────────────────────────────────────
var source = await ReadSourceAsync(sourceConnection);
var result = LegacyTransform.Transform(source);
foreach (var note in result.Notes)
    Console.WriteLine($"  {note}");
Console.WriteLine();

// ── 2. Write the target in one transaction ──────────────────────────────────
await using var pg = new NpgsqlConnection(targetConnection);
await pg.OpenAsync();
await using var tx = await pg.BeginTransactionAsync();

await EnsureTargetReadyAsync(pg, tx);

foreach (var table in LegacyTransform.TableOrder)
{
    var rows = result.Data.Rows(table);
    var columns = await TargetColumnsAsync(pg, tx, table);

    var unknown = rows.SelectMany(r => r.Keys).Distinct(StringComparer.OrdinalIgnoreCase)
        .Where(c => !columns.Contains(c)).ToList();
    if (unknown.Count > 0)
        throw new InvalidOperationException(
            $"{table}: source column(s) {string.Join(", ", unknown)} have no place in the target — refusing to drop data.");

    await InsertAsync(pg, tx, table, rows);
    await ResetIdentityAsync(pg, tx, table, columns);
}

// ── 3. Verify ───────────────────────────────────────────────────────────────
Console.WriteLine($"{"Table",-22}{"Source",8}{"Expected",10}{"Target",8}");
var mismatch = false;
foreach (var table in LegacyTransform.TableOrder)
{
    var inTarget = await CountAsync(pg, tx, table);
    var expected = result.Data.Rows(table).Count;
    mismatch |= inTarget != expected;
    Console.WriteLine($"{table,-22}{source.Rows(table).Count,8}{expected,10}{inTarget,8}{(inTarget == expected ? "" : "  <-- MISMATCH")}");
}

var totals = await ScalarAsync(pg, tx,
    "SELECT concat(count(*), ' users, wallet total ', coalesce(sum(\"WalletBalance\"), 0), ' VND, ', " +
    "count(*) FILTER (WHERE \"ProExpiresAt\" > now()), ' with active PRO') FROM \"Users\"");
Console.WriteLine();
Console.WriteLine($"Users: {totals}");

if (mismatch)
{
    await tx.RollbackAsync();
    Console.Error.WriteLine("Row counts do not match — rolled back.");
    return 1;
}

if (apply)
{
    await tx.CommitAsync();
    Console.WriteLine("Committed.");
}
else
{
    await tx.RollbackAsync();
    Console.WriteLine("Dry run OK — everything was written and checked, then rolled back. Re-run with --apply to commit.");
}
return 0;

// ── Helpers ─────────────────────────────────────────────────────────────────

static async Task<Snapshot> ReadSourceAsync(string connectionString)
{
    var snapshot = new Snapshot();
    await using var sql = new SqlConnection(connectionString);
    await sql.OpenAsync();

    // One consistent point in time across all tables. Azure SQL allows snapshot isolation by default;
    // a local SQL Server usually does not, and then tables are read one after another.
    await using var check = new SqlCommand(
        "SELECT snapshot_isolation_state FROM sys.databases WHERE name = DB_NAME()", sql);
    var snapshotAllowed = Convert.ToInt32(await check.ExecuteScalarAsync()) == 1;

    SqlTransaction? tx = snapshotAllowed ? sql.BeginTransaction(IsolationLevel.Snapshot) : null;
    if (!snapshotAllowed)
        Console.WriteLine("  (snapshot isolation not enabled on the source — reading tables one after another)");

    foreach (var table in LegacyTransform.TableOrder)
    {
        await using var cmd = new SqlCommand($"SELECT * FROM [{table}]", sql, tx);
        await using var reader = await cmd.ExecuteReaderAsync();
        var rows = new List<Row>();
        while (await reader.ReadAsync())
        {
            var row = new Row();
            for (var i = 0; i < reader.FieldCount; i++)
                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            rows.Add(row);
        }
        snapshot[table] = rows;
    }

    if (tx != null)
    {
        tx.Commit();   // read-only transaction; nothing to write
        tx.Dispose();
    }
    return snapshot;
}

static async Task EnsureTargetReadyAsync(NpgsqlConnection pg, NpgsqlTransaction tx)
{
    var migrated = await ScalarAsync(pg, tx,
        "SELECT count(*) FROM information_schema.tables WHERE table_name = '__EFMigrationsHistory'");
    if (Convert.ToInt64(migrated) == 0)
        throw new InvalidOperationException("Target has no schema. Run: dotnet ef database update (InitialPostgres) first.");

    foreach (var table in LegacyTransform.TableOrder)
    {
        if (await CountAsync(pg, tx, table) > 0)
            throw new InvalidOperationException($"Target table {table} is not empty — refusing to mix data.");
    }
}

static async Task<HashSet<string>> TargetColumnsAsync(NpgsqlConnection pg, NpgsqlTransaction tx, string table)
{
    var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    await using var cmd = new NpgsqlCommand(
        "SELECT column_name FROM information_schema.columns WHERE table_schema = 'public' AND table_name = @t", pg, tx);
    cmd.Parameters.AddWithValue("t", table);
    await using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
        columns.Add(reader.GetString(0));
    return columns;
}

static async Task InsertAsync(NpgsqlConnection pg, NpgsqlTransaction tx, string table, List<Row> rows)
{
    if (rows.Count == 0) return;

    // One round trip per table: Neon is remote, so per-row round trips would be slow.
    await using var batch = new NpgsqlBatch(pg, tx);
    foreach (var row in rows)
    {
        var columns = row.Keys.ToList();
        var command = new NpgsqlBatchCommand(
            $"INSERT INTO \"{table}\" ({string.Join(", ", columns.Select(c => $"\"{c}\""))}) " +
            $"OVERRIDING SYSTEM VALUE VALUES ({string.Join(", ", columns.Select((_, i) => $"@p{i}"))})");
        for (var i = 0; i < columns.Count; i++)
            command.Parameters.Add(new NpgsqlParameter($"p{i}", row[columns[i]] ?? DBNull.Value));
        batch.BatchCommands.Add(command);
    }
    await batch.ExecuteNonQueryAsync();
}

static async Task ResetIdentityAsync(NpgsqlConnection pg, NpgsqlTransaction tx, string table, HashSet<string> columns)
{
    if (!columns.Contains("Id")) return;   // transactions uses a string key

    // Make the next generated Id follow the copied ones.
    await ScalarAsync(pg, tx,
        $"SELECT setval(pg_get_serial_sequence('\"{table}\"', 'Id'), coalesce(max(\"Id\"), 1), max(\"Id\") IS NOT NULL) FROM \"{table}\"");
}

static async Task<long> CountAsync(NpgsqlConnection pg, NpgsqlTransaction tx, string table)
    => Convert.ToInt64(await ScalarAsync(pg, tx, $"SELECT count(*) FROM \"{table}\""));

static async Task<object?> ScalarAsync(NpgsqlConnection pg, NpgsqlTransaction tx, string sql)
{
    await using var cmd = new NpgsqlCommand(sql, pg, tx);
    return await cmd.ExecuteScalarAsync();
}

static string DescribeSqlServer(string cs)
{
    var b = new SqlConnectionStringBuilder(cs);
    return $"{b.DataSource} / {b.InitialCatalog}";
}

static string DescribePostgres(string cs)
{
    var b = new NpgsqlConnectionStringBuilder(cs);
    return $"{b.Host} / {b.Database}";
}
