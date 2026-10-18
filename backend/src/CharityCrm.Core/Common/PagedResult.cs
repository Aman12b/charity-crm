namespace CharityCrm.Core.Common;

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

public static class Paging
{
    public const int DefaultPageSize = 25;
    public const int MaxPageSize = 100;

    public static int NormalizePage(int? page) => page is null or < 1 ? 1 : page.Value;

    public static int NormalizePageSize(int? pageSize) =>
        pageSize is null or < 1 ? DefaultPageSize : Math.Min(pageSize.Value, MaxPageSize);
}
