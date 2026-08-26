using System.Net;
using EPCLeadGenerator.Api.Database;
using EPCLeadGenerator.Api.Services; // Namespace for your IPostcodeLookupService
using Microsoft.EntityFrameworkCore;

namespace EPCLeadGenerator.Api.Features.Postcodes;

public class GetPostcodeDeprivation
{
    public record Request(string Postcode);

    public record PostcodeDeprivationResponse(
        string Postcode,
        bool MarkAsDone,
        string? LSOACode,
        string? LSOAName,
        decimal? MultipleDeprivationPercentage,
        int? MultipleDeprivationDecile
    );

    public record Response(PostcodeDeprivationResponse? Data);

    public class Endpoint(DataContext dataContext, IPostcodeLookupService postcodeService)
        : Endpoint<Request, Response>
    {
        public override void Configure()
        {
            Post("/postcodes/lookup");
            AllowAnonymous();

            Description(b =>
                b.ProducesProblemFE<ProblemDetails>(404, "application/problem+json")
                    .ProducesProblemFE<ProblemDetails>(502, "application/problem+json")
            );

            Summary(s =>
            {
                s.Responses[200] = "The request was successful.";
                s.Responses[404] = "The requested resource was not found.";
                s.Responses[502] = "An upstream service gateway error occurred.";
            });
        }

        public override async Task<Response> ExecuteAsync(Request req, CancellationToken ct)
        {
            var cleanPostcode = req.Postcode.Trim().ToUpper();

            var postcodeRecord = await dataContext
                .Postcodes.Include(p => p.LSOADeprivation)
                .FirstOrDefaultAsync(p => p.PostcodeKey.ToUpper() == cleanPostcode, ct);

            if (postcodeRecord is null)
            {
                postcodeRecord = new Postcode { PostcodeKey = cleanPostcode, MarkAsDone = false };

                dataContext.Postcodes.Add(postcodeRecord);
            }

            if (string.IsNullOrEmpty(postcodeRecord.LSOACode))
            {
                var lookup = await postcodeService.GetLSOAAsync(cleanPostcode, ct);

                if (!lookup.IsSuccess)
                {
                    ThrowError(
                        lookup.ErrorMessage ?? "Failed to lookup postcode externally.",
                        lookup.StatusCode == 404 ? 404 : 502
                    );
                }

                postcodeRecord.LSOACode = lookup.LSOACode;

                postcodeRecord.LSOADeprivation =
                    await dataContext.LSOADeprivation.FirstOrDefaultAsync(
                        l => l.LSOACode == lookup.LSOACode,
                        ct
                    );
            }

            await dataContext.SaveChangesAsync(ct);

            var lsoa = postcodeRecord.LSOADeprivation;

            return new Response(
                new PostcodeDeprivationResponse(
                    Postcode: postcodeRecord.PostcodeKey,
                    MarkAsDone: postcodeRecord.MarkAsDone,
                    LSOACode: postcodeRecord.LSOACode,
                    LSOAName: lsoa?.LSOAName,
                    MultipleDeprivationPercentage: lsoa?.MultipleDeprivationPercentage,
                    MultipleDeprivationDecile: lsoa?.MultipleDeprivationDecile
                )
            );
        }
    }
}
