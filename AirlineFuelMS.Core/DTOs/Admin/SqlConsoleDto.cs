namespace AirlineFuelMS.Core.DTOs.Admin;

public record SqlQueryRequest(string Query);

public record SqlQueryResult(
    bool Success,
    string Provider,                       // "Sqlite" or "Postgres"
    long ElapsedMs,
    int RowCount,
    IReadOnlyList<string> Columns,
    IReadOnlyList<IReadOnlyList<object?>> Rows,
    string? Error
);

public record TableInfoDto(string TableName, IReadOnlyList<ColumnInfoDto> Columns);
public record ColumnInfoDto(string Name, string Type, bool Nullable);
