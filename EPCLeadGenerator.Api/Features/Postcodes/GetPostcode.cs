using EPCLeadGenerator.Api.Database;
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

    public class Endpoint(DataContext dataContext) : Endpoint<Request, Response>
    {
        public override void Configure()
        {
            Post("/postcodes/lookup");
            AllowAnonymous();
            Description(b => b.ProducesProblemFE<ProblemDetails>(404));
        }

        public override async Task<Response> ExecuteAsync(Request req, CancellationToken ct)
        {
            var postcodeRecord = await dataContext
                .Postcodes.Include(p => p.LSOADeprivation)
                .FirstOrDefaultAsync(p => p.PostcodeKey.ToUpper() == req.Postcode.ToUpper(), ct);

            if (postcodeRecord is null)
            {
                ThrowError("Postcode not found in database", 404);
            }

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
