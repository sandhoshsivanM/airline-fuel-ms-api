namespace AirlineFuelMS.Core.DTOs.Common;

public record PagedResult<T>(
    IEnumerable<T> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
)
{
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;

    public static PagedResult<T> Create(IEnumerable<T> items, int totalCount, int page, int pageSize)
    {
        var totalPages = pageSize <= 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);
        return new PagedResult<T>(items, totalCount, page, pageSize, totalPages);
    }
}
