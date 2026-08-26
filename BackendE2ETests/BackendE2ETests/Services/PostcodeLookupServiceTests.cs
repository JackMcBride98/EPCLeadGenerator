using System.Net.Http.Json;
using EPCLeadGenerator.Api.Services;

namespace Tests.Services;

public class PostcodeLookupServiceTests
{
    private static (
        PostcodeLookupService Service,
        HttpMessageHandlerStub Handler
    ) CreateServiceWithMockHttp(HttpStatusCode statusCode, HttpContent content)
    {
        var handler = new HttpMessageHandlerStub(statusCode, content);
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://example.com/api") };
        var service = new PostcodeLookupService(client);
        return (service, handler);
    }

    private static (
        PostcodeLookupService Service,
        HttpMessageHandlerThrowingStub Handler
    ) CreateServiceWithThrowingHttp(Exception exceptionToThrow)
    {
        var handler = new HttpMessageHandlerThrowingStub(exceptionToThrow);
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://example.com/api") };
        var service = new PostcodeLookupService(client);
        return (service, handler);
    }

    [Fact]
    public async Task GetLSOAAsync_WhenApiReturnsSuccessWithLSOA_ReturnsSuccessResult()
    {
        // Arrange
        var apiResponse = new PostcodesIoResponse(
            new PostcodeResult(new PostcodeCodes("E01012345"))
        );
        var content = JsonContent.Create(apiResponse);
        var (service, _) = CreateServiceWithMockHttp(HttpStatusCode.OK, content);

        // Act
        var result = await service.GetLSOAAsync("BS81QU", CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.StatusCode.ShouldBe(200);
        result.LSOACode.ShouldBe("E01012345");
        result.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public async Task GetLSOAAsync_WhenApiReturns404_ReturnsNotFoundResult()
    {
        // Arrange
        var content = new StringContent("{}");
        var (service, _) = CreateServiceWithMockHttp(HttpStatusCode.NotFound, content);

        // Act
        var result = await service.GetLSOAAsync("XX99XX", CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.StatusCode.ShouldBe(404);
        result.LSOACode.ShouldBeNull();
        result.ErrorMessage.ShouldNotBeNull();
        result.ErrorMessage.ShouldContain("was not found on postcodes.io");
    }

    [Fact]
    public async Task GetLSOAAsync_WhenApiReturnsNonSuccessStatusCode_ReturnsErrorResult()
    {
        // Arrange
        var content = new StringContent("Server Error");
        var (service, _) = CreateServiceWithMockHttp(HttpStatusCode.InternalServerError, content);

        // Act
        var result = await service.GetLSOAAsync("BS81QU", CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.StatusCode.ShouldBe(500);
        result.LSOACode.ShouldBeNull();
        result.ErrorMessage.ShouldNotBeNull();
        result.ErrorMessage.ShouldContain("unexpected status code: 500");
    }

    [Fact]
    public async Task GetLSOAAsync_WhenLsoaCodeIsMissingInResponse_ReturnsUnprocessableEntityResult()
    {
        // Arrange - Valid JSON 200 OK, but Codes or Lsoa is missing/null
        var apiResponse = new PostcodesIoResponse(new PostcodeResult(new PostcodeCodes(null)));
        var content = JsonContent.Create(apiResponse);
        var (service, _) = CreateServiceWithMockHttp(HttpStatusCode.OK, content);

        // Act
        var result = await service.GetLSOAAsync("BS81QU", CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.StatusCode.ShouldBe(422);
        result.LSOACode.ShouldBeNull();
        result.ErrorMessage.ShouldNotBeNull();
        result.ErrorMessage.ShouldContain("LSOA code was missing");
    }

    [Fact]
    public async Task GetLSOAAsync_WhenResponseThrowsJsonException_ReturnsBadGatewayResult()
    {
        // Arrange - Malformed JSON causing a JsonException when reading
        var content = new StringContent("Not a valid json response");
        var (service, _) = CreateServiceWithMockHttp(HttpStatusCode.OK, content);

        // Act
        var result = await service.GetLSOAAsync("BS81QU", CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.StatusCode.ShouldBe(502);
        result.LSOACode.ShouldBeNull();
        result.ErrorMessage.ShouldNotBeNull();
        result.ErrorMessage.ShouldContain("Failed to parse the response");
    }

    [Fact]
    public async Task GetLSOAAsync_WhenNetworkThrowsHttpRequestException_ReturnsServiceUnavailableResult()
    {
        // Arrange - Network failure throwing HttpRequestException
        var (service, _) = CreateServiceWithThrowingHttp(new HttpRequestException("Network down"));

        // Act
        var result = await service.GetLSOAAsync("BS81QU", CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.StatusCode.ShouldBe(503);
        result.LSOACode.ShouldBeNull();
        result.ErrorMessage.ShouldNotBeNull();
        result.ErrorMessage.ShouldContain("Network error while communicating");
    }
}

internal class HttpMessageHandlerStub(HttpStatusCode statusCode, HttpContent content)
    : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        var response = new HttpResponseMessage(statusCode) { Content = content };
        return Task.FromResult(response);
    }
}

internal class HttpMessageHandlerThrowingStub(Exception exceptionToThrow) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        throw exceptionToThrow;
    }
}
