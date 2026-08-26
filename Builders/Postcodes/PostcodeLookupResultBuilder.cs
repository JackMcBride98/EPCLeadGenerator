using Bogus;
using EPCLeadGenerator.Api.Services;

namespace Builders.Postcodes;

public class PostcodeLookupResultBuilder : Builder<PostcodeLookupResult>
{
    private static readonly Faker Faker = new();

    public bool IsSuccess { get; set; } = true;
    public string? LSOACode { get; set; } = $"E010{Faker.Random.Number(10000, 99999)}";
    public string? ErrorMessage { get; set; }
    public int StatusCode { get; set; } = 200;

    public PostcodeLookupResultBuilder WithErrorMessage(
        string errorMessage = "Failed to lookup postcode.",
        int statusCode = 502
    )
    {
        IsSuccess = false;
        LSOACode = null;
        ErrorMessage = errorMessage;
        StatusCode = statusCode;
        return this;
    }

    public PostcodeLookupResultBuilder WithSuccess(string lsoaCode)
    {
        IsSuccess = true;
        LSOACode = lsoaCode;
        ErrorMessage = null;
        StatusCode = 200;
        return this;
    }

    public override PostcodeLookupResult Build()
    {
        return new PostcodeLookupResult(IsSuccess, LSOACode, ErrorMessage, StatusCode);
    }
}
