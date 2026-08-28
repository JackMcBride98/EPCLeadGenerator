using EPCLeadGenerator.Api.Database;
using LinqKit;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace EPCLeadGenerator.Api.Features.Postcodes;

public class SearchProcessedPostcodes
{
    private static readonly string[] EpcRatings = ["A", "B", "C", "D", "E", "F", "G"];

    public record Request(string? PostcodeSearchTerm);

    public record Response(List<PostcodeResponse> Postcodes);

    public record PostcodeResponse(
        string Postcode,
        string EPCsLastUpdatedAt,
        LSOADeprivationResponse LSOADeprivation,
        EPCAssessmentAggregationResponse EPCAggregation,
        List<EPCAssessmentResponse> EPCAssessments
    );

    public record LSOADeprivationResponse(
        decimal MultipleDeprivationPercentage,
        int MultipleDeprivationRank,
        decimal IncomeDeprivationPercentage,
        int IncomeDeprivationRank,
        decimal EmploymentDeprivationPercentage,
        int EmploymentDeprivationRank,
        decimal BarriersToHousingAndServicesPercentage,
        int BarriersToHousingAndServicesRank
    );

    public record EPCAssessmentAggregationResponse(
        int TotalAssessments,
        decimal PercentageExpired,
        decimal PercentageExpiringInNextYear,
        Dictionary<string, decimal> EPCRatingPercentages
    );

    public record EPCAssessmentResponse(
        long EPCAssessmentId,
        string AddressLine,
        string? EPCRating,
        string UniquePropertyReferenceNumber,
        string CertificateNumber,
        DateTime RegistrationDate,
        bool IsLatest,
        bool IsExpired,
        bool IsExpiringInNextYear
    );

    public class Endpoint(DataContext dataContext) : Endpoint<Request, Response>
    {
        public override void Configure()
        {
            Get("/postcodes/processed");
            AllowAnonymous();

            Description(b => b.ProducesProblemDetails(400).ProducesProblemDetails(500));

            Summary(s =>
            {
                s.Responses[200] = "The processed postcodes were retrieved successfully.";
                s.Responses[400] = "Bad Request - Invalid request payload.";
                s.Responses[500] = "An internal server error occurred.";
            });
        }

        public override async Task<Response> ExecuteAsync(Request req, CancellationToken ct)
        {
            var uppercasePostcodeSearchTerm = req.PostcodeSearchTerm?.Trim().ToUpperInvariant();
            var now = SystemClock.Instance.GetCurrentInstant();

            var processedPostcodes = await dataContext
                .Postcodes.AsExpandable()
                .AsSplitQuery()
                .Where(p => p.LSOACode != null && p.EPCsLastUpdatedAt != null)
                .Where(p =>
                    string.IsNullOrWhiteSpace(uppercasePostcodeSearchTerm)
                    || p.PostcodeKey.StartsWith(uppercasePostcodeSearchTerm)
                )
                .Select(p => new
                {
                    p.PostcodeKey,
                    EPCsLastUpdatedAtString = p.EPCsLastUpdatedAt.ToString(),

                    LSOA = p.LSOADeprivation,

                    TotalCount = p.EPCAssessments.Count(a => a.IsLatest),

                    ExpiredCount = p
                        .EPCAssessments.AsQueryable()
                        .Where(a => a.IsLatest)
                        .Count(a => EPCAssessment.IsExpiredExpression.Invoke(a, now)),

                    ExpiringSoonCount = p
                        .EPCAssessments.AsQueryable()
                        .Where(a => a.IsLatest)
                        .Count(a => EPCAssessment.IsExpiringInNextYearExpression.Invoke(a, now)),

                    RatingCounts = p
                        .EPCAssessments.Where(a => a.IsLatest && a.EPCRating != null)
                        .GroupBy(a => a.EPCRating!)
                        .Select(g => new { Rating = g.Key, Count = g.Count() })
                        .ToList(),

                    Assessments = p
                        .EPCAssessments.OrderBy(a => a.UniquePropertyReferenceNumber)
                        .Select(a => new EPCAssessmentResponse(
                            a.EPCAssessmentId,
                            a.AddressLine,
                            a.EPCRating,
                            a.UniquePropertyReferenceNumber.ToString(),
                            a.CertificateNumber,
                            a.RegistrationDate.ToDateTimeUtc(),
                            a.IsLatest,
                            EPCAssessment.IsExpiredExpression.Invoke(a, now),
                            EPCAssessment.IsExpiringInNextYearExpression.Invoke(a, now)
                        ))
                        .ToList(),
                })
                .ToListAsync(ct);

            return new Response(
                processedPostcodes
                    .Select(p => new PostcodeResponse(
                        p.PostcodeKey,
                        p.EPCsLastUpdatedAtString!,
                        new LSOADeprivationResponse(
                            p.LSOA!.MultipleDeprivationPercentage,
                            p.LSOA.MultipleDeprivationRank,
                            p.LSOA.IncomePercentage,
                            p.LSOA.IncomeRank,
                            p.LSOA.EmploymentPercentage,
                            p.LSOA.EmploymentRank,
                            p.LSOA.BarriersToHousingAndServicesPercentage,
                            p.LSOA.BarriersToHousingAndServicesRank
                        ),
                        new EPCAssessmentAggregationResponse(
                            p.TotalCount,
                            CalculatePercentage(p.ExpiredCount, p.TotalCount),
                            CalculatePercentage(p.ExpiringSoonCount, p.TotalCount),
                            BuildRatingPercentages(
                                p.RatingCounts.ToDictionary(r => r.Rating, r => r.Count),
                                p.TotalCount
                            )
                        ),
                        p.Assessments
                    ))
                    .ToList()
            );
        }

        private static Dictionary<string, decimal> BuildRatingPercentages(
            Dictionary<string, int> actualCounts,
            int totalCount
        )
        {
            return EpcRatings.ToDictionary(
                rating => rating,
                rating => CalculatePercentage(actualCounts.GetValueOrDefault(rating, 0), totalCount)
            );
        }

        private static decimal CalculatePercentage(int count, int total) =>
            total == 0 ? 0 : Math.Round((decimal)count / total * 100, 2);
    }
}
