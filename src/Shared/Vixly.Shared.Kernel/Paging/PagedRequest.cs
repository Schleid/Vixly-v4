namespace Vixly.Shared.Kernel.Paging;

// =========================================================
// FILE: PagedRequest.cs
//
// PURPOSE:
// A page and page size for a list read.
//
// RESPONSIBILITIES:
// - Keep every list read bounded (Global Standard P-1)
// - Apply one default page size and one hard cap everywhere
//
// IMPORTANT NOTES:
// - Out-of-range input is clamped, never rejected, so a
//   malformed query string cannot trigger an unbounded read
// - Pages are 1-based
// =========================================================
public sealed record PagedRequest
{
    public const int DefaultPageSize = 25;
    public const int MaximumPageSize = 100;

    private PagedRequest(int page, int pageSize)
    {
        Page = page;
        PageSize = pageSize;
    }

    public int Page { get; }

    public int PageSize { get; }

    public int Skip => (Page - 1) * PageSize;

    // =====================================================
    // METHOD: CREATE
    // =====================================================
    public static PagedRequest Create(int page, int pageSize)
    {
        int safePage = Math.Max(page, 1);

        int safePageSize = pageSize < 1
            ? DefaultPageSize
            : Math.Min(pageSize, MaximumPageSize);

        return new PagedRequest(safePage, safePageSize);
    }

    public static PagedRequest First => Create(1, DefaultPageSize);
}
