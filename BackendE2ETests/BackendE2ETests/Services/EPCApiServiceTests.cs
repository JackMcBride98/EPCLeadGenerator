using System.Text.Json;
using Builders.EPCApi;
using EPCLeadGenerator.Api.Services;
using Microsoft.Extensions.Configuration;
using RichardSzalay.MockHttp;

namespace Tests.Services;

public class EPCApiServiceTests
{
    private const string ApiBaseUrl =
        "https://api.get-energy-performance-data.communities.gov.uk/api";
    private const string TestBearerToken = "test-bearer-token";
    private readonly IConfiguration _configuration;

    public EPCApiServiceTests()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "EPCApi:BearerToken", TestBearerToken },
        };

        _configuration = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
    }

    [Fact]
    public async Task SearchCertificatesByPostcodeAsync_ShouldThrowInvalidOperationException_WhenBearerTokenIsMissing()
    {
        // Arrange
        var emptyConfig = new ConfigurationBuilder().Build();
        var client = new HttpClient();
        var service = new EPCApiService(client, emptyConfig);

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            service.SearchCertificatesByPostcodeAsync("AB1 2CD", CancellationToken.None)
        );
        exception.Message.ShouldBe("Bearer token is not configured.");
    }

    [Fact]
    public async Task SearchCertificatesByPostcodeAsync_ShouldReturnSuccess_WhenSinglePageReturned()
    {
        // Arrange
        var mockResponse = new EPCSearchResponseBuilder
        {
            Data = new List<EPCCertificate>
            {
                new EPCCertificateBuilder
                {
                    CertificateNumber = "CERT-123",
                    AddressLine1 = "123 Fake St",
                    Postcode = "AB1 2CD",
                }.Build(),
            },
            Pagination = new EPCPaginationBuilder
            {
                TotalRecords = 1,
                CurrentPage = 1,
                TotalPages = 1,
                NextPage = null,
            }.Build(),
        }.Build();

        using var mockHttp = new MockHttpMessageHandler();
        mockHttp.Fallback.Throw(new InvalidOperationException());

        mockHttp
            .When($"{ApiBaseUrl}/domestic/search")
            .WithQueryString("postcode", "AB1 2CD")
            .WithHeaders("Authorization", $"Bearer {TestBearerToken}")
            .Respond("application/json", JsonSerializer.Serialize(mockResponse));

        var client = mockHttp.ToHttpClient();
        var service = new EPCApiService(client, _configuration);

        // Act
        var result = await service.SearchCertificatesByPostcodeAsync(
            "AB1 2CD",
            CancellationToken.None
        );

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.StatusCode.ShouldBe(200);
        result.Certificates.ShouldNotBeNull();
        result.Certificates.Count.ShouldBe(1);
        result.Certificates[0].CertificateNumber.ShouldBe("CERT-123");
        result.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public async Task SearchCertificatesByPostcodeAsync_ShouldHandlePagination_WhenMultiplePagesExist()
    {
        // Arrange
        var page1Response = new EPCSearchResponseBuilder
        {
            Data = new List<EPCCertificate>
            {
                new EPCCertificateBuilder { CertificateNumber = "CERT-1" }.Build(),
            },
            Pagination = new EPCPaginationBuilder
            {
                TotalRecords = 2,
                CurrentPage = 1,
                TotalPages = 2,
                NextPage = new UriBuilder(
                    $"{ApiBaseUrl}/domestic/search?postcode=AB1 2CD&page=2"
                ).Uri.ToString(),
            }.Build(),
        }.Build();

        var page2Response = new EPCSearchResponseBuilder
        {
            Data = new List<EPCCertificate>
            {
                new EPCCertificateBuilder { CertificateNumber = "CERT-2" }.Build(),
            },
            Pagination = new EPCPaginationBuilder
            {
                TotalRecords = 2,
                CurrentPage = 2,
                TotalPages = 2,
                NextPage = null,
            }.Build(),
        }.Build();

        using var mockHttp = new MockHttpMessageHandler();

        mockHttp
            .When($"{ApiBaseUrl}/domestic/search")
            .WithQueryString("postcode", "AB1 2CD")
            .WithQueryString("page", "2")
            .WithHeaders("Authorization", $"Bearer {TestBearerToken}")
            .Respond("application/json", JsonSerializer.Serialize(page2Response));

        mockHttp
            .When($"{ApiBaseUrl}/domestic/search")
            .WithQueryString("postcode", "AB1 2CD")
            .WithHeaders("Authorization", $"Bearer {TestBearerToken}")
            .Respond("application/json", JsonSerializer.Serialize(page1Response));

        var client = mockHttp.ToHttpClient();
        var service = new EPCApiService(client, _configuration);

        // Act
        var result = await service.SearchCertificatesByPostcodeAsync(
            "AB1 2CD",
            CancellationToken.None
        );

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Certificates.ShouldNotBeNull();
        result.Certificates.Count.ShouldBe(2);
        result.Certificates[0].CertificateNumber.ShouldBe("CERT-1");
        result.Certificates[1].CertificateNumber.ShouldBe("CERT-2");
    }

    [Fact]
    public async Task SearchCertificatesByPostcodeAsync_ShouldReturnErrorResult_WhenUpstreamReturnsFailureStatus()
    {
        // Arrange
        using var mockHttp = new MockHttpMessageHandler();

        mockHttp
            .When($"{ApiBaseUrl}/domestic/search")
            .WithQueryString("postcode", "INVALID")
            .Respond(HttpStatusCode.BadRequest, "text/plain", "Invalid postcode format");

        var client = mockHttp.ToHttpClient();
        var service = new EPCApiService(client, _configuration);

        // Act
        var result = await service.SearchCertificatesByPostcodeAsync(
            "INVALID",
            CancellationToken.None
        );

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.StatusCode.ShouldBe(400);
        result.ErrorMessage.ShouldBe("Invalid postcode format");
        result.Certificates.ShouldBeNull();
    }

    [Fact]
    public async Task SearchCertificatesByPostcodeAsync_ShouldReturn502_WhenHttpRequestExceptionOccurs()
    {
        // Arrange
        using var mockHttp = new MockHttpMessageHandler();
        mockHttp.Fallback.Throw(new InvalidOperationException());

        mockHttp
            .When($"{ApiBaseUrl}/domestic/search")
            .Throw(new HttpRequestException("Network down"));

        var client = mockHttp.ToHttpClient();
        var service = new EPCApiService(client, _configuration);

        // Act
        var result = await service.SearchCertificatesByPostcodeAsync(
            "AB1 2CD",
            CancellationToken.None
        );

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.StatusCode.ShouldBe(502);
        result.ErrorMessage.ShouldNotBeNull();
        result.ErrorMessage.Contains("A network communication error occurred").ShouldBeTrue();
    }

    [Fact]
    public async Task SearchCertificatesByPostcodeAsync_ShouldReturn502_WhenTaskCanceledExceptionOccurs()
    {
        // Arrange
        using var mockHttp = new MockHttpMessageHandler();
        mockHttp.Fallback.Throw(new InvalidOperationException());

        mockHttp.When($"{ApiBaseUrl}/domestic/search").Throw(new TaskCanceledException("Timeout"));

        var client = mockHttp.ToHttpClient();
        var service = new EPCApiService(client, _configuration);

        // Act
        var result = await service.SearchCertificatesByPostcodeAsync(
            "AB1 2CD",
            CancellationToken.None
        );

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.StatusCode.ShouldBe(502);
        result.ErrorMessage.ShouldNotBeNull();
        result.ErrorMessage.Contains("timed out").ShouldBeTrue();
    }

    [Fact]
    public async Task SearchCertificatesByPostcodeAsync_ShouldReturn502_WhenJsonExceptionOccurs()
    {
        // Arrange
        using var mockHttp = new MockHttpMessageHandler();
        mockHttp.Fallback.Throw(new InvalidOperationException());

        mockHttp
            .When($"{ApiBaseUrl}/domestic/search")
            .Respond("application/json", "Malformed JSON");

        var client = mockHttp.ToHttpClient();
        var service = new EPCApiService(client, _configuration);

        // Act
        var result = await service.SearchCertificatesByPostcodeAsync(
            "AB1 2CD",
            CancellationToken.None
        );

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.StatusCode.ShouldBe(502);
        result.ErrorMessage.ShouldNotBeNull();
        result.ErrorMessage.Contains("Failed to parse the response").ShouldBeTrue();
    }
}
