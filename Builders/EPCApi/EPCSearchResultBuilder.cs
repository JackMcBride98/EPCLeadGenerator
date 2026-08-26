using Bogus;
using EPCLeadGenerator.Api.Services;

namespace Builders.EPCApi;

public class EPCSearchResultBuilder : Builder<EPCSearchResult>
{
    private static readonly Faker Faker = new();

    public bool IsSuccess { get; set; } = true;
    public List<EPCCertificate>? Certificates { get; set; } =
        new() { new EPCCertificateBuilder().Build() };
    public string? ErrorMessage { get; set; } = null;
    public int StatusCode { get; set; } = 200;

    public override EPCSearchResult Build()
    {
        return new EPCSearchResult(IsSuccess, Certificates, ErrorMessage, StatusCode);
    }
}
