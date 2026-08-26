using EPCLeadGenerator.Api.Services;

namespace Builders.EPCApi;

public class EPCSearchResponseBuilder : Builder<EPCSearchResponse>
{
    public List<EPCCertificate>? Data { get; set; } = new() { new EPCCertificateBuilder().Build() };
    public EPCPagination? Pagination { get; set; } = new EPCPaginationBuilder().Build();

    public override EPCSearchResponse Build() => new(Data: Data, Pagination: Pagination);
}
