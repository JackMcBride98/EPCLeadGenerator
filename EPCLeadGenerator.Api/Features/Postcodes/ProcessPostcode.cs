using EPCLeadGenerator.Api.Database;
using EPCLeadGenerator.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NodaTime;
using NodaTime.Extensions;
using NodaTime.Text;

namespace EPCLeadGenerator.Api.Features.Postcodes;

public class ProcessPostcode
{
    public record Request(string Postcode, bool RefreshEPCData = false);

    public record Response(string Postcode, string Message);

    public class Endpoint(
        DataContext dataContext,
        IPostcodeLookupService postcodeService,
        IEPCApiService epcApiService,
        ILogger<Endpoint> logger
    ) : Endpoint<Request, Response>
    {
        public override void Configure()
        {
            Post("/postcodes/process");
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
                .Include(p => p.EPCAssessments)
                .FirstOrDefaultAsync(p => p.PostcodeKey == cleanPostcode, ct);

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
                await dataContext.SaveChangesAsync(ct);
            }

            var postcodeHasExistingEPCData = postcodeRecord.EPCAssessments.Count != 0;

            if (!postcodeHasExistingEPCData || req.RefreshEPCData)
            {
                var epcResult = await epcApiService.SearchCertificatesByPostcodeAsync(
                    cleanPostcode,
                    ct
                );

                if (!epcResult.IsSuccess)
                {
                    ThrowError(
                        epcResult.ErrorMessage ?? "Failed to lookup EPC certificates externally.",
                        epcResult.StatusCode == 404 ? 404 : 502
                    );
                }

                if (epcResult.Certificates == null)
                {
                    ThrowError(
                        $"EPC Certificate search returned null for Postcode {cleanPostcode}.",
                        404
                    );
                }

                var validCertificates = FilterValidCertificates(
                    epcResult.Certificates,
                    cleanPostcode,
                    logger
                );

                if (postcodeHasExistingEPCData)
                {
                    dataContext.EPCAssessments.RemoveRange(postcodeRecord.EPCAssessments);
                }

                var newAssessments = MapAndFlagLatestAssessments(validCertificates, cleanPostcode);

                dataContext.EPCAssessments.AddRange(newAssessments);

                postcodeRecord.EPCsLastUpdatedAt = SystemClock.Instance.GetCurrentInstant();

                await dataContext.SaveChangesAsync(ct);
            }

            return new Response(cleanPostcode, "Postcode processed successfully.");
        }
    }

    private static List<EPCCertificate> FilterValidCertificates(
        List<EPCCertificate> certificates,
        string postcode,
        ILogger logger
    )
    {
        var validCertificates = new List<EPCCertificate>();

        foreach (var cert in certificates)
        {
            if (!cert.Uprn.HasValue)
            {
                logger.LogWarning(
                    "Skipping EPC certificate {CertificateNumber} for postcode {Postcode} because it lacks a UPRN.",
                    cert.CertificateNumber,
                    postcode
                );
                continue;
            }

            validCertificates.Add(cert);
        }

        return validCertificates;
    }

    private static List<EPCAssessment> MapAndFlagLatestAssessments(
        List<EPCCertificate> certificates,
        string postcodeKey
    )
    {
        var now = DateTime.UtcNow.ToInstant();

        var assessments = certificates
            .Select(c => new EPCAssessment
            {
                PostcodeKey = postcodeKey,
                AddressLine = c.AddressLine1 ?? "No Address Line 1 Found",
                EPCRating = c.CurrentEnergyEfficiencyBand,
                RegistrationDate = ParseRegistrationDateToInstant(c.RegistrationDate) ?? now,
                UpdatedAt = now,
                UniquePropertyReferenceNumber = c.Uprn!.Value,
                CertificateNumber = c.CertificateNumber,
                IsLatest = false,
            })
            .ToList();

        var groupedByUprn = assessments.GroupBy(a => a.UniquePropertyReferenceNumber);

        foreach (var group in groupedByUprn)
        {
            var latestInGroup = group.OrderByDescending(a => a.RegistrationDate).First();
            latestInGroup.IsLatest = true;
        }

        return assessments;
    }

    private static Instant? ParseRegistrationDateToInstant(string? dateStr)
    {
        if (string.IsNullOrWhiteSpace(dateStr))
            return null;

        var parseResult = LocalDatePattern.Iso.Parse(dateStr);
        if (parseResult.Success)
        {
            return parseResult.Value.AtMidnight().InUtc().ToInstant();
        }

        if (DateTime.TryParse(dateStr, out var parsedDateTime))
        {
            return LocalDate.FromDateTime(parsedDateTime).AtMidnight().InUtc().ToInstant();
        }

        return null;
    }
}
