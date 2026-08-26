using System.Text.Json;

namespace EPCLeadGenerator.Api.Services;

public record PostcodesIoResponse(PostcodeResult? Result);

public record PostcodeResult(PostcodeCodes? Codes);

public record PostcodeCodes(string? LSOA);

public record PostcodeLookupResult(
    bool IsSuccess,
    string? LSOACode,
    string? ErrorMessage,
    int StatusCode = 200
);

public interface IPostcodeLookupService
{
    Task<PostcodeLookupResult> GetLSOAAsync(string postcode, CancellationToken cancellationToken);
}

public class PostcodeLookupService(HttpClient client) : IPostcodeLookupService
{
    private const string BaseUrl = "https://api.postcodes.io/postcodes/";

    public async Task<PostcodeLookupResult> GetLSOAAsync(
        string postcode,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var response = await client.GetAsync(
                $"{BaseUrl}{Uri.EscapeDataString(postcode)}",
                cancellationToken
            );

            if ((int)response.StatusCode == 404)
            {
                return new PostcodeLookupResult(
                    false,
                    null,
                    $"The postcode '{postcode}' was not found on postcodes.io.",
                    404
                );
            }

            if (!response.IsSuccessStatusCode)
            {
                return new PostcodeLookupResult(
                    false,
                    null,
                    $"Postcodes.io returned an unexpected status code: {(int)response.StatusCode}.",
                    (int)response.StatusCode
                );
            }

            var data = await response.Content.ReadFromJsonAsync<PostcodesIoResponse>(
                cancellationToken: cancellationToken
            );
            var lsoa = data?.Result?.Codes?.LSOA;

            if (string.IsNullOrEmpty(lsoa))
            {
                return new PostcodeLookupResult(
                    false,
                    null,
                    $"Postcodes.io successfully found the postcode '{postcode}', but the LSOA code was missing from the response.",
                    422
                );
            }

            return new PostcodeLookupResult(true, lsoa, null);
        }
        catch (JsonException ex)
        {
            return new PostcodeLookupResult(
                false,
                null,
                $"Failed to parse the response from postcodes.io: {ex.Message}",
                502
            );
        }
        catch (HttpRequestException ex)
        {
            return new PostcodeLookupResult(
                false,
                null,
                $"Network error while communicating with postcodes.io: {ex.Message}",
                503
            );
        }
    }
}
