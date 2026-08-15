namespace Dompet.Api.DTOs;

public record PageResult<T>(
    List<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages
);