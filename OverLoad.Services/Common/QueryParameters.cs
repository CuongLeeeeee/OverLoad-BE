namespace OverLoad.Services.Common;

/// <summary>
/// Shared paging and sorting input. Out-of-range values are clamped instead of rejected, and
/// <see cref="SortBy"/> follows the "field" / "-field" convention (a leading '-' sorts descending).
/// </summary>
public abstract class QueryParameters
{
    private const int MaxPageSize = 100;

    private int _pageNumber = 1;
    public int PageNumber
    {
        get => _pageNumber;
        set => _pageNumber = value < 1 ? 1 : value;
    }

    private int _pageSize = 10;
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value switch
        {
            < 1 => 10,
            > MaxPageSize => MaxPageSize,
            _ => value
        };
    }

    public abstract string? SortBy { get; set; }

    /// <summary>Splits <see cref="SortBy"/> into a lower-case field name and a direction.</summary>
    public (string? Field, bool Descending) ParseSort()
    {
        if (string.IsNullOrWhiteSpace(SortBy)) return (null, false);

        var sort = SortBy.Trim();
        var descending = sort.StartsWith('-');
        var field = (descending ? sort[1..] : sort).ToLowerInvariant();
        return (field, descending);
    }
}
