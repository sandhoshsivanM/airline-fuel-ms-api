namespace AirlineFuelMS.Core.DTOs.Common;

/// <summary>
/// Common query envelope for all paginated list endpoints.
/// </summary>
public class PagedQuery
{
    private const int MaxPageSize = 200;

    private int _page = 1;
    public int Page
    {
        get => _page;
        init => _page = value < 1 ? 1 : value;
    }

    private int _pageSize = 20;
    public int PageSize
    {
        get => _pageSize;
        init => _pageSize = value switch
        {
            < 1 => 20,
            > MaxPageSize => MaxPageSize,
            _ => value
        };
    }

    /// <summary>Field name to sort by (case-insensitive). Each service defines what it accepts.</summary>
    public string? SortBy { get; init; }

    /// <summary>"asc" (default) or "desc".</summary>
    public string? SortDirection { get; init; } = "asc";

    /// <summary>Free-text search applied to entity-specific name/code-like fields.</summary>
    public string? Search { get; init; }

    public bool IsDescending =>
        string.Equals(SortDirection, "desc", StringComparison.OrdinalIgnoreCase);
}
