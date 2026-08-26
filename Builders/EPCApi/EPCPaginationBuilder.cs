using EPCLeadGenerator.Api.Services;

namespace Builders.EPCApi;

public class EPCPaginationBuilder : Builder<EPCPagination>
{
    public int TotalRecords { get; set; } = 1;
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public string? NextPage { get; set; } = null;
    public int PageSize { get; set; } = 25;

    public override EPCPagination Build() =>
        new(
            TotalRecords: TotalRecords,
            CurrentPage: CurrentPage,
            TotalPages: TotalPages,
            NextPage: NextPage,
            PrevPage: null,
            PageSize: PageSize
        );
}
