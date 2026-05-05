using System.Data.Common;
using System.Diagnostics;
using System.Text.RegularExpressions;
using AirlineFuelMS.Core.DTOs.Admin;
using AirlineFuelMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AirlineFuelMS.Infrastructure.Services;

public interface ISqlConsoleService
{
    Task<SqlQueryResult> RunReadOnlyAsync(string query, CancellationToken ct = default);
    Task<IReadOnlyList<TableInfoDto>> GetSchemaAsync(CancellationToken ct = default);
    string Provider { get; }
}

public class SqlConsoleService : ISqlConsoleService
{
    private readonly AppDbContext _context;
    private const int MaxRows = 1000;
    private const int TimeoutSeconds = 15;

    // Forbidden top-level keywords. We only allow SELECT (and WITH for CTEs).
    private static readonly Regex ForbiddenStartRegex = new(
        @"^\s*(insert|update|delete|drop|truncate|alter|create|grant|revoke|exec|execute|merge|call|do)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex MultiStatementRegex = new(
        @";\s*\S",   // a semicolon followed by more SQL (trailing single ; is OK)
        RegexOptions.Compiled);

    public SqlConsoleService(AppDbContext context) => _context = context;

    public string Provider => _context.Database.ProviderName?.Contains("Sqlite") == true ? "Sqlite" : "Postgres";

    public async Task<SqlQueryResult> RunReadOnlyAsync(string rawQuery, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();

        // ---- Safety guards ----
        if (string.IsNullOrWhiteSpace(rawQuery))
            return Fail("Query is empty.");

        var query = rawQuery.Trim().TrimEnd(';').Trim();

        if (ForbiddenStartRegex.IsMatch(query))
            return Fail("Only read-only queries (SELECT / WITH) are allowed in the console.");
        if (MultiStatementRegex.IsMatch(query))
            return Fail("Multiple statements aren't allowed — run one query at a time.");

        // ---- Execute via raw ADO.NET so we can return arbitrary columns ----
        var conn = _context.Database.GetDbConnection();
        var opened = false;
        try
        {
            if (conn.State != System.Data.ConnectionState.Open)
            {
                await conn.OpenAsync(ct);
                opened = true;
            }

            using var cmd = conn.CreateCommand();
            cmd.CommandText = query;
            cmd.CommandTimeout = TimeoutSeconds;

            using var reader = await cmd.ExecuteReaderAsync(ct);

            var columns = Enumerable.Range(0, reader.FieldCount)
                .Select(reader.GetName).ToList();

            var rows = new List<IReadOnlyList<object?>>();
            while (await reader.ReadAsync(ct) && rows.Count < MaxRows)
            {
                var row = new object?[reader.FieldCount];
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var v = reader.GetValue(i);
                    row[i] = v is DBNull ? null : v;
                }
                rows.Add(row);
            }

            sw.Stop();
            return new SqlQueryResult(
                Success: true, Provider: Provider, ElapsedMs: sw.ElapsedMilliseconds,
                RowCount: rows.Count, Columns: columns, Rows: rows, Error: null
            );
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new SqlQueryResult(
                Success: false, Provider: Provider, ElapsedMs: sw.ElapsedMilliseconds,
                RowCount: 0, Columns: Array.Empty<string>(),
                Rows: Array.Empty<IReadOnlyList<object?>>(), Error: ex.Message
            );
        }
        finally
        {
            if (opened) await conn.CloseAsync();
        }

        SqlQueryResult Fail(string err) => new(
            false, Provider, sw.ElapsedMilliseconds, 0,
            Array.Empty<string>(), Array.Empty<IReadOnlyList<object?>>(), err);
    }

    public async Task<IReadOnlyList<TableInfoDto>> GetSchemaAsync(CancellationToken ct = default)
    {
        var conn = _context.Database.GetDbConnection();
        var opened = false;
        try
        {
            if (conn.State != System.Data.ConnectionState.Open)
            {
                await conn.OpenAsync(ct);
                opened = true;
            }

            // ADO.NET schema metadata works for both Sqlite and Postgres
            var tables = await conn.GetSchemaAsync("Tables", ct);
            var columns = await conn.GetSchemaAsync("Columns", ct);

            var result = new List<TableInfoDto>();

            foreach (System.Data.DataRow t in tables.Rows)
            {
                var tableName = t["TABLE_NAME"]?.ToString();
                var tableType = t.Table.Columns.Contains("TABLE_TYPE") ? t["TABLE_TYPE"]?.ToString() : null;

                if (string.IsNullOrEmpty(tableName)) continue;
                // Skip system / migration history tables
                if (tableName.StartsWith("__")) continue;
                if (tableName.StartsWith("sqlite_")) continue;
                if (tableType is not null &&
                    !tableType.Equals("TABLE", StringComparison.OrdinalIgnoreCase) &&
                    !tableType.Equals("BASE TABLE", StringComparison.OrdinalIgnoreCase))
                    continue;

                var cols = new List<ColumnInfoDto>();
                foreach (System.Data.DataRow c in columns.Rows)
                {
                    if (!string.Equals(c["TABLE_NAME"]?.ToString(), tableName, StringComparison.Ordinal))
                        continue;

                    var colName = c["COLUMN_NAME"]?.ToString() ?? "";
                    var dataType = c.Table.Columns.Contains("DATA_TYPE") ? c["DATA_TYPE"]?.ToString() ?? "" : "";
                    var nullable = c.Table.Columns.Contains("IS_NULLABLE")
                        ? string.Equals(c["IS_NULLABLE"]?.ToString(), "YES", StringComparison.OrdinalIgnoreCase)
                        : true;
                    cols.Add(new ColumnInfoDto(colName, dataType, nullable));
                }

                result.Add(new TableInfoDto(tableName, cols));
            }

            return result.OrderBy(x => x.TableName).ToList();
        }
        finally
        {
            if (opened) await conn.CloseAsync();
        }
    }
}
