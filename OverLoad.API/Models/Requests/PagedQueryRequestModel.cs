namespace OverLoad.API.Models.Requests;

/// <summary>Paging and sorting accepted by every list endpoint.</summary>
public abstract class PagedQueryRequestModel
{
    /// <summary>1-based page number. Values below 1 are treated as 1.</summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>Items per page (default 10, max 100).</summary>
    public int PageSize { get; set; } = 10;

    /// <summary>Field to sort by; prefix with '-' for descending. The allowed fields are listed per endpoint.</summary>
    public string? SortBy { get; set; }
}
