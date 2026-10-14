using Vixly.Shared.Kernel.Paging;

namespace Vixly.Shared.Kernel.Tests;

// =========================================================
// FILE: PagingTests.cs
//
// PURPOSE:
// Verifies that paging stays bounded (Global Standard P-1).
//
// RESPONSIBILITIES:
// - Cover clamping of page and page size
// - Cover page-count arithmetic of PagedResult
//
// IMPORTANT NOTES:
// - The hard cap is a security and performance guarantee;
//   changing it needs an ADR
// =========================================================
public sealed class PagingTests
{
    [Theory]
    [InlineData(1, 10, 1, 10)]
    [InlineData(0, 10, 1, 10)]
    [InlineData(-5, 10, 1, 10)]
    [InlineData(3, 0, 3, PagedRequest.DefaultPageSize)]
    [InlineData(3, -1, 3, PagedRequest.DefaultPageSize)]
    [InlineData(2, 100000, 2, PagedRequest.MaximumPageSize)]
    public void Create_ClampsToSafeBounds(int page, int size, int expectedPage, int expectedSize)
    {
        PagedRequest request = PagedRequest.Create(page, size);

        Assert.Equal(expectedPage, request.Page);
        Assert.Equal(expectedSize, request.PageSize);
    }

    [Fact]
    public void Skip_IsZeroBasedOffset()
    {
        Assert.Equal(40, PagedRequest.Create(3, 20).Skip);
    }

    [Theory]
    [InlineData(0, 0, false)]
    [InlineData(1, 1, false)]
    [InlineData(20, 1, false)]
    [InlineData(21, 2, true)]
    [InlineData(100, 5, true)]
    public void Result_ComputesTotalPagesAndHasNext(int total, int expectedPages, bool hasNext)
    {
        PagedResult<int> result = new(
            new List<int>(),
            PagedRequest.Create(1, 20),
            total);

        Assert.Equal(expectedPages, result.TotalPages);
        Assert.Equal(hasNext, result.HasNextPage);
    }

    [Fact]
    public void Result_RejectsNegativeTotals()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new PagedResult<int>(new List<int>(), PagedRequest.First, -1));
    }

    [Fact]
    public void Empty_HasNoItemsAndNoPages()
    {
        PagedResult<string> empty = PagedResult<string>.Empty(PagedRequest.First);

        Assert.Empty(empty.Items);
        Assert.Equal(0, empty.TotalPages);
    }
}
