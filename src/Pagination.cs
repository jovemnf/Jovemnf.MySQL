using System;
using System.Collections.Generic;

namespace Jovemnf.MySQL;

public readonly struct PageRequest(int page, int pageSize)
{
    public int Page { get; } = page < 1 ? throw new ArgumentOutOfRangeException(nameof(page)) : page;
    public int PageSize { get; } = pageSize < 1 ? throw new ArgumentOutOfRangeException(nameof(pageSize)) : pageSize;
    public int Offset => (Page - 1) * PageSize;
}

public sealed class PagedResult<T>
{
    public required IReadOnlyList<T> Items { get; init; }
    public required long TotalItems { get; init; }
    public required int Page { get; init; }
    public required int PageSize { get; init; }
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalItems / (double)PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}
