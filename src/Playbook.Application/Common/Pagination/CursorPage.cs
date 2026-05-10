namespace Playbook.Application.Common.Pagination;

public sealed record CursorPage<T>(IReadOnlyList<T> Items, string? EndCursor, bool HasNextPage);
