namespace Vixly.Shared.Kernel.Paging;

// =========================================================
// FILE: PagedResult.cs
//
// PURPOSE:
// One page of a list read plus the total match count.
//
// RESPONSIBILITIES:
// - Carry the items of the requested page
// - Carry the paging facts a UI needs (total, page count)
//
// IMPORTANT NOTES:
// - Items are projections, never tracked entities
// - Immutable once created
// =========================================================
public sealed record PagedResult<T>
{
    public PagedResult(
        IReadOnlyList<T> items,
        PagedRequest request,
        int totalCount)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentOutOfRangeException.ThrowIfNegative(totalCount);

        Items = items;
        Page = request.Page;
        PageSize = request.PageSize;
        TotalCount = totalCount;
    }

    public IReadOnlyList<T> Items { get; }

    public int Page { get; }

    public int PageSize { get; }

    public int TotalCount { get; }

    public int TotalPages => (TotalCount + PageSize - 1) / PageSize;

    public bool HasNextPage => Page < TotalPages;

    public static PagedResult<T> Empty(PagedRequest request)
    {
        return new PagedResult<T>(Array.Empty<T>(), request, 0);
    }
}
