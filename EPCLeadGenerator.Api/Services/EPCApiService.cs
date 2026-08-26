using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;

namespace EPCLeadGenerator.Api.Services;

public record EPCCertificate(
    [property: JsonPropertyName("certificateNumber")] string CertificateNumber,
    [property: JsonPropertyName("addressLine1")] string? AddressLine1,
    [property: JsonPropertyName("addressLine2")] string? AddressLine2,
    [property: JsonPropertyName("addressLine3")] string? AddressLine3,
    [property: JsonPropertyName("addressLine4")] string? AddressLine4,
    [property: JsonPropertyName("postcode")] string? Postcode,
    [property: JsonPropertyName("postTown")] string? PostTown,
    [property: JsonPropertyName("council")] string? Council,
    [property: JsonPropertyName("constituency")] string? Constituency,
    [property: JsonPropertyName("currentEnergyEfficiencyBand")] string? CurrentEnergyEfficiencyBand,
    [property: JsonPropertyName("registrationDate")] string? RegistrationDate,
    [property: JsonPropertyName("uprn")] long? Uprn,
    [property: JsonPropertyName("schemaType")] string? SchemaType
);

public record EPCPagination(
    [property: JsonPropertyName("totalRecords")] int TotalRecords,
    [property: JsonPropertyName("currentPage")] int CurrentPage,
    [property: JsonPropertyName("totalPages")] int TotalPages,
    [property: JsonPropertyName("nextPage")] string? NextPage,
    [property: JsonPropertyName("prevPage")] string? PrevPage,
    [property: JsonPropertyName("pageSize")] int PageSize
);

public record EPCSearchResponse(
    [property: JsonPropertyName("data")] List<EPCCertificate>? Data,
    [property: JsonPropertyName("pagination")] EPCPagination? Pagination
);

public record EPCSearchResult(
    bool IsSuccess,
    List<EPCCertificate>? Certificates,
    string? ErrorMessage,
    int StatusCode = 200
);

public interface IEPCApiService
{
    Task<EPCSearchResult> SearchCertificatesByPostcodeAsync(
        string postcode,
        CancellationToken cancellationToken
    );
}

public class EPCApiService : IEPCApiService
{
    private readonly HttpClient _client;
    private readonly IConfiguration _configuration;
    private const string BaseUrl = "https://api.get-energy-performance-data.communities.gov.uk/api";

    public EPCApiService(HttpClient client, IConfiguration configuration)
    {
        _client = client;
        _configuration = configuration;
    }

    public async Task<EPCSearchResult> SearchCertificatesByPostcodeAsync(
        string postcode,
        CancellationToken cancellationToken
    )
    {
        var token = _configuration["EPCApi:BearerToken"];
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException("Bearer token is not configured.");
        }

        try
        {
            var allCertificates = new List<EPCCertificate>();
            var nextUrl = $"{BaseUrl}/domestic/search?postcode={Uri.EscapeDataString(postcode)}";

            while (!string.IsNullOrEmpty(nextUrl))
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, nextUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                using var response = await _client.SendAsync(request, cancellationToken);
                var statusCode = (int)response.StatusCode;

                if (!response.IsSuccessStatusCode)
                {
                    string errorContent;
                    try
                    {
                        errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    }
                    catch
                    {
                        errorContent = $"Upstream returned status code {statusCode}";
                    }

                    return new EPCSearchResult(false, null, errorContent, statusCode);
                }

                var pageData = await response.Content.ReadFromJsonAsync<EPCSearchResponse>(
                    cancellationToken: cancellationToken
                );

                if (pageData?.Data != null)
                {
                    allCertificates.AddRange(pageData.Data);
                }

                nextUrl = pageData?.Pagination?.NextPage;
            }

            return new EPCSearchResult(true, allCertificates, null);
        }
        catch (JsonException ex)
        {
            return new EPCSearchResult(
                false,
                null,
                $"Failed to parse the response from the upstream service: {ex.Message}",
                502
            );
        }
        catch (HttpRequestException ex)
        {
            return new EPCSearchResult(
                false,
                null,
                $"A network communication error occurred while reaching the upstream service: {ex.Message}",
                502
            );
        }
        catch (TaskCanceledException ex)
        {
            return new EPCSearchResult(
                false,
                null,
                $"The request to the upstream service timed out: {ex.Message}",
                502
            );
        }
        catch (Exception ex)
        {
            return new EPCSearchResult(
                false,
                null,
                $"An unexpected error occurred while processing the certificate search: {ex.Message}",
                502
            );
        }
    }
}
