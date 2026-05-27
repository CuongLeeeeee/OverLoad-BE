namespace OverLoad.Services.Common;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public List<string> Errors { get; set; } = new();

    public static ApiResponse<T> SuccessResult(T data, string message = "Operation successful")
        => new() { Success = true, Message = message, Data = data };

    public static ApiResponse<T> FailResult(string message, List<string>? errors = null)
        => new() { Success = false, Message = message, Errors = errors ?? new() };

    public static ApiResponse<T> FailResult(string message, string error)
        => new() { Success = false, Message = message, Errors = new List<string> { error } };
}

public class PagedResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public IEnumerable<T> Data { get; set; } = Enumerable.Empty<T>();
    public PaginationMeta Pagination { get; set; } = new();
    public List<string> Errors { get; set; } = new();

    public static PagedResponse<T> SuccessResult(
        IEnumerable<T> data, int totalCount, int page, int pageSize, string message = "Data retrieved successfully")
        => new()
        {
            Success = true,
            Message = message,
            Data = data,
            Pagination = new PaginationMeta
            {
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            }
        };

    public static PagedResponse<T> FailResult(string message, List<string>? errors = null)
        => new() { Success = false, Message = message, Errors = errors ?? new() };
}

public class PaginationMeta
{
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}
