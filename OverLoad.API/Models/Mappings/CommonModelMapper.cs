using OverLoad.API.Models.Requests;
using OverLoad.API.Models.Responses;
using OverLoad.Services.Common;

namespace OverLoad.API.Models.Mappings;

public static class CommonModelMapper
{
    /// <summary>Copies paging input; an omitted SortBy keeps the service's default order.</summary>
    public static TQuery WithPaging<TQuery>(this TQuery query, PagedQueryRequestModel model)
        where TQuery : QueryParameters
    {
        query.PageNumber = model.PageNumber;
        query.PageSize = model.PageSize;
        if (!string.IsNullOrWhiteSpace(model.SortBy))
            query.SortBy = model.SortBy;
        return query;
    }

    public static PagedResponseModel<TModel> ToResponseModel<TDto, TModel>(
        this PagedResult<TDto> paged, Func<TDto, TModel> map) => new()
    {
        Items = paged.Items.Select(map).ToList(),
        PageNumber = paged.PageNumber,
        PageSize = paged.PageSize,
        TotalCount = paged.TotalCount,
        TotalPages = paged.TotalPages,
        HasPreviousPage = paged.HasPreviousPage,
        HasNextPage = paged.HasNextPage
    };
}
