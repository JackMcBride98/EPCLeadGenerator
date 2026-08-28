using Builders;
using Builders.Postcodes;
using EPCLeadGenerator.Api.Features.Postcodes;
using NodaTime;

namespace Tests.Features.Postcodes;

public class SearchProcessedPostcodesTests(App app) : TestBase(app)
{
    // ==========================================
    // 1. Data Mapping Tests
    // ==========================================

    [Fact]
    public async Task SearchProcessedPostcodes_ValidPostcodeMatch_CorrectlyMapsLSOADeprivationResponse()
    {
        // Arrange
        const string postcodeKey = "BS1 1AA";
        const string lsoaCode = "E01014421";

        const int expectedMultipleDeprivationRank = 1500;
        const decimal expectedMultipleDeprivationPercentage = 12.34m;
        const int expectedIncomeRank = 2000;
        const decimal expectedIncomePercentage = 15.50m;
        const int expectedEmploymentRank = 1800;
        const decimal expectedEmploymentPercentage = 11.20m;
        const int expectedBarriersToHousingAndServicesRank = 500;
        const decimal expectedBarriersToHousingAndServicesPercentage = 45.67m;

        var lsoaDeprivation = new LSOADeprivationBuilder
        {
            LSOACode = lsoaCode,
            MultipleDeprivationRank = expectedMultipleDeprivationRank,
            MultipleDeprivationPercentage = expectedMultipleDeprivationPercentage,
            IncomeRank = expectedIncomeRank,
            IncomePercentage = expectedIncomePercentage,
            EmploymentRank = expectedEmploymentRank,
            EmploymentPercentage = expectedEmploymentPercentage,
            BarriersToHousingAndServicesRank = expectedBarriersToHousingAndServicesRank,
            BarriersToHousingAndServicesPercentage = expectedBarriersToHousingAndServicesPercentage,
        }.Build();

        var postcode = new PostcodeBuilder { PostcodeKey = postcodeKey, LSOACode = lsoaCode }
            .WithLSOADeprivationData(lsoaCode)
            .Build();

        postcode.LSOADeprivation = lsoaDeprivation;
        postcode.EPCsLastUpdatedAt = SystemClock.Instance.GetCurrentInstant();

        Db.LSOADeprivation.Add(lsoaDeprivation);
        Db.Postcodes.Add(postcode);
        await Db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var request = new SearchProcessedPostcodes.Request(postcodeKey);

        // Act
        var (response, result) = await App.Client.GETAsync<
            SearchProcessedPostcodes.Endpoint,
            SearchProcessedPostcodes.Request,
            SearchProcessedPostcodes.Response
        >(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.ShouldNotBeNull();

        var postcodeResult = result.Postcodes.ShouldHaveSingleItem();
        postcodeResult.Postcode.ShouldBe(postcodeKey);

        var lsoa = postcodeResult.LSOADeprivation;
        lsoa.ShouldNotBeNull();
        lsoa.MultipleDeprivationRank.ShouldBe(expectedMultipleDeprivationRank);
        lsoa.MultipleDeprivationPercentage.ShouldBe(expectedMultipleDeprivationPercentage);
        lsoa.IncomeDeprivationRank.ShouldBe(expectedIncomeRank);
        lsoa.IncomeDeprivationPercentage.ShouldBe(expectedIncomePercentage);
        lsoa.EmploymentDeprivationRank.ShouldBe(expectedEmploymentRank);
        lsoa.EmploymentDeprivationPercentage.ShouldBe(expectedEmploymentPercentage);
        lsoa.BarriersToHousingAndServicesRank.ShouldBe(expectedBarriersToHousingAndServicesRank);
        lsoa.BarriersToHousingAndServicesPercentage.ShouldBe(
            expectedBarriersToHousingAndServicesPercentage
        );
    }

    [Fact]
    public async Task SearchProcessedPostcodes_ValidPostcodeMatch_CorrectlyMapsEPCAssessmentResponseList()
    {
        // Arrange
        const string postcodeKey = "BS1 2BB";
        const string lsoaCode = "E01014421";

        const long activeUprn = 100012345678;
        const string activeCertNo = "CERT-ACTIVE-001";
        const string activeAddress = "10 Active Street";
        const string activeRating = "B";

        const long expiredUprn = 100087654321;
        const string expiredCertNo = "CERT-EXPIRED-002";

        const long expiringSoonUprn = 100011223344;
        const string expiringSoonCertNo = "CERT-EXPIRING-003";

        const string historicalCertNo = "CERT-HISTORICAL-004";

        var postcode = new PostcodeBuilder { PostcodeKey = postcodeKey, LSOACode = lsoaCode }
            .WithLSOADeprivationData(lsoaCode)
            .Build();

        postcode.EPCsLastUpdatedAt = SystemClock.Instance.GetCurrentInstant();

        // 1. Standard active assessment (IsLatest = true, valid for ~5 more years)
        var activeAssessment = new EPCAssessmentBuilder
        {
            PostcodeKey = postcodeKey,
            UniquePropertyReferenceNumber = activeUprn,
            CertificateNumber = activeCertNo,
            AddressLine = activeAddress,
            EPCRating = activeRating,
            Postcode = postcode,
        }
            .WithIsLatest(true)
            .Build();

        // 2. Expired assessment (>10 years old)
        var expiredAssessment = new EPCAssessmentBuilder
        {
            PostcodeKey = postcodeKey,
            UniquePropertyReferenceNumber = expiredUprn,
            CertificateNumber = expiredCertNo,
            Postcode = postcode,
        }
            .WithIsLatest(true)
            .WithExpired()
            .Build();

        // 3. Expiring soon assessment (between 9 and 10 years old)
        var expiringSoonAssessment = new EPCAssessmentBuilder
        {
            PostcodeKey = postcodeKey,
            UniquePropertyReferenceNumber = expiringSoonUprn,
            CertificateNumber = expiringSoonCertNo,
            Postcode = postcode,
        }
            .WithIsLatest(true)
            .WithExpiringInNextYear()
            .Build();

        // 4. Non-latest (historical) assessment sharing the SAME UPRN as activeAssessment
        var historicalAssessment = new EPCAssessmentBuilder
        {
            PostcodeKey = postcodeKey,
            UniquePropertyReferenceNumber = activeUprn,
            CertificateNumber = historicalCertNo,
            AddressLine = activeAddress,
            Postcode = postcode,
            RegistrationDate = activeAssessment.RegistrationDate.Minus(Duration.FromDays(365 * 10)),
        }
            .WithIsLatest(false)
            .Build();

        postcode.EPCAssessments.Add(activeAssessment);
        postcode.EPCAssessments.Add(expiredAssessment);
        postcode.EPCAssessments.Add(expiringSoonAssessment);
        postcode.EPCAssessments.Add(historicalAssessment);

        Db.Postcodes.Add(postcode);
        await Db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var request = new SearchProcessedPostcodes.Request(postcodeKey);

        // Act
        var (response, result) = await App.Client.GETAsync<
            SearchProcessedPostcodes.Endpoint,
            SearchProcessedPostcodes.Request,
            SearchProcessedPostcodes.Response
        >(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.ShouldNotBeNull();

        var postcodeResult = result.Postcodes.ShouldHaveSingleItem();
        postcodeResult.EPCAssessments.Count.ShouldBe(4);

        // Verify Active Assessment
        var mappedActive = postcodeResult.EPCAssessments.Single(a =>
            a.CertificateNumber == activeCertNo
        );
        mappedActive.AddressLine.ShouldBe(activeAddress);
        mappedActive.EPCRating.ShouldBe(activeRating);
        mappedActive.UniquePropertyReferenceNumber.ShouldBe(activeUprn.ToString());
        mappedActive.IsLatest.ShouldBeTrue();
        mappedActive.IsExpired.ShouldBeFalse();
        mappedActive.IsExpiringInNextYear.ShouldBeFalse();

        // Verify Expired Assessment
        var mappedExpired = postcodeResult.EPCAssessments.Single(a =>
            a.CertificateNumber == expiredCertNo
        );
        mappedExpired.IsLatest.ShouldBeTrue();
        mappedExpired.IsExpired.ShouldBeTrue();
        mappedExpired.IsExpiringInNextYear.ShouldBeFalse();

        // Verify Expiring Soon Assessment
        var mappedExpiringSoon = postcodeResult.EPCAssessments.Single(a =>
            a.CertificateNumber == expiringSoonCertNo
        );
        mappedExpiringSoon.IsLatest.ShouldBeTrue();
        mappedExpiringSoon.IsExpired.ShouldBeFalse();
        mappedExpiringSoon.IsExpiringInNextYear.ShouldBeTrue();

        // Verify Historical Assessment (shares activeUprn, marked IsLatest = false)
        var mappedHistorical = postcodeResult.EPCAssessments.Single(a =>
            a.CertificateNumber == historicalCertNo
        );
        mappedHistorical.UniquePropertyReferenceNumber.ShouldBe(activeUprn.ToString());
        mappedHistorical.IsLatest.ShouldBeFalse();
    }

    // ==========================================
    // 2. Aggregation & Edge Cases
    // ==========================================

    [Fact]
    public async Task SearchProcessedPostcodes_WithValidAssessments_CalculatesPercentagesAndRatingCountsCorrectly()
    {
        // Arrange
        const string postcodeKey = "BS1 3AG";
        const string lsoaCode = "E01014421";

        var postcode = new PostcodeBuilder { PostcodeKey = postcodeKey, LSOACode = lsoaCode }
            .WithLSOADeprivationData(lsoaCode)
            .Build();

        postcode.EPCsLastUpdatedAt = SystemClock.Instance.GetCurrentInstant();

        // Total 4 active/latest assessments:
        // - 1 Expired (Rating A) -> 25%
        // - 1 Expiring Soon (Rating B) -> 25%
        // - 2 Valid/Active (Rating B, Rating C) -> 50% valid
        // Rating Distribution: A=25% (1/4), B=50% (2/4), C=25% (1/4), D-G=0%

        var expiredAssessment = new EPCAssessmentBuilder
        {
            PostcodeKey = postcodeKey,
            EPCRating = "A",
            Postcode = postcode,
        }
            .WithIsLatest(true)
            .WithExpired()
            .Build();

        var expiringSoonAssessment = new EPCAssessmentBuilder
        {
            PostcodeKey = postcodeKey,
            EPCRating = "B",
            Postcode = postcode,
        }
            .WithIsLatest(true)
            .WithExpiringInNextYear()
            .Build();

        var validAssessment1 = new EPCAssessmentBuilder
        {
            PostcodeKey = postcodeKey,
            EPCRating = "B",
            Postcode = postcode,
        }
            .WithIsLatest(true)
            .Build();

        var validAssessment2 = new EPCAssessmentBuilder
        {
            PostcodeKey = postcodeKey,
            EPCRating = "C",
            Postcode = postcode,
        }
            .WithIsLatest(true)
            .Build();

        postcode.EPCAssessments.Add(expiredAssessment);
        postcode.EPCAssessments.Add(expiringSoonAssessment);
        postcode.EPCAssessments.Add(validAssessment1);
        postcode.EPCAssessments.Add(validAssessment2);

        Db.Postcodes.Add(postcode);
        await Db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var request = new SearchProcessedPostcodes.Request(postcodeKey);

        // Act
        var (response, result) = await App.Client.GETAsync<
            SearchProcessedPostcodes.Endpoint,
            SearchProcessedPostcodes.Request,
            SearchProcessedPostcodes.Response
        >(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.ShouldNotBeNull();

        var postcodeResult = result.Postcodes.ShouldHaveSingleItem();
        var agg = postcodeResult.EPCAggregation;

        agg.TotalAssessments.ShouldBe(4);
        agg.PercentageExpired.ShouldBe(25.00m);
        agg.PercentageExpiringInNextYear.ShouldBe(25.00m);

        // Verify A-G EPC Rating Dictionary
        agg.EPCRatingPercentages["A"].ShouldBe(25.00m);
        agg.EPCRatingPercentages["B"].ShouldBe(50.00m);
        agg.EPCRatingPercentages["C"].ShouldBe(25.00m);
        agg.EPCRatingPercentages["D"].ShouldBe(0.00m);
        agg.EPCRatingPercentages["E"].ShouldBe(0.00m);
        agg.EPCRatingPercentages["F"].ShouldBe(0.00m);
        agg.EPCRatingPercentages["G"].ShouldBe(0.00m);
    }

    [Fact]
    public async Task SearchProcessedPostcodes_PostcodeHasZeroAssessments_ReturnsZeroPercentagesAndEmptyRatingDistribution()
    {
        // Arrange
        const string postcodeKey = "BS1 0NO";
        const string lsoaCode = "E01014421";

        var postcode = new PostcodeBuilder { PostcodeKey = postcodeKey, LSOACode = lsoaCode }
            .WithLSOADeprivationData(lsoaCode)
            .Build();

        postcode.EPCsLastUpdatedAt = SystemClock.Instance.GetCurrentInstant();
        postcode.EPCAssessments.Clear();

        Db.Postcodes.Add(postcode);
        await Db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var request = new SearchProcessedPostcodes.Request(postcodeKey);

        // Act
        var (response, result) = await App.Client.GETAsync<
            SearchProcessedPostcodes.Endpoint,
            SearchProcessedPostcodes.Request,
            SearchProcessedPostcodes.Response
        >(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.ShouldNotBeNull();

        var postcodeResult = result.Postcodes.ShouldHaveSingleItem();
        var agg = postcodeResult.EPCAggregation;

        agg.TotalAssessments.ShouldBe(0);
        agg.PercentageExpired.ShouldBe(0.00m);
        agg.PercentageExpiringInNextYear.ShouldBe(0.00m);

        // All ratings A-G should map to 0% without division-by-zero exceptions
        agg.EPCRatingPercentages.Count.ShouldBe(7);
        agg.EPCRatingPercentages.Values.All(p => p == 0.00m).ShouldBeTrue();
    }

    [Fact]
    public async Task SearchProcessedPostcodes_HasHistoricalNonLatestAssessments_ExcludesHistoricalFromAggregationCounts()
    {
        // Arrange
        const string postcodeKey = "BS1 4HST";
        const string lsoaCode = "E01014421";
        const long mainUprn = 100099887766;
        const long secondaryUprn = 200011223344;

        var postcode = new PostcodeBuilder { PostcodeKey = postcodeKey, LSOACode = lsoaCode }
            .WithLSOADeprivationData(lsoaCode)
            .Build();

        postcode.EPCsLastUpdatedAt = SystemClock.Instance.GetCurrentInstant();

        // 1 active assessment for main UPRN (Rating A, Valid)
        var latestAssessment = new EPCAssessmentBuilder
        {
            PostcodeKey = postcodeKey,
            UniquePropertyReferenceNumber = mainUprn,
            EPCRating = "A",
            Postcode = postcode,
        }
            .WithIsLatest(true)
            .Build();

        // 2 historical assessments sharing main UPRN (both expired, Rating G)
        var historical1 = new EPCAssessmentBuilder
        {
            PostcodeKey = postcodeKey,
            UniquePropertyReferenceNumber = mainUprn,
            EPCRating = "G",
            Postcode = postcode,
        }
            .WithIsLatest(false)
            .WithExpired()
            .Build();

        var historical2 = new EPCAssessmentBuilder
        {
            PostcodeKey = postcodeKey,
            UniquePropertyReferenceNumber = mainUprn,
            EPCRating = "G",
            Postcode = postcode,
        }
            .WithIsLatest(false)
            .WithExpiringInNextYear()
            .Build();

        // 1 active assessment for secondary UPRN (Rating C, Expiring Soon)
        var secondaryExpiringSoonAssessment = new EPCAssessmentBuilder
        {
            PostcodeKey = postcodeKey,
            UniquePropertyReferenceNumber = secondaryUprn,
            EPCRating = "C",
            Postcode = postcode,
        }
            .WithIsLatest(true)
            .WithExpiringInNextYear()
            .Build();

        postcode.EPCAssessments.Add(latestAssessment);
        postcode.EPCAssessments.Add(historical1);
        postcode.EPCAssessments.Add(historical2);
        postcode.EPCAssessments.Add(secondaryExpiringSoonAssessment);

        Db.Postcodes.Add(postcode);
        await Db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var request = new SearchProcessedPostcodes.Request(postcodeKey);

        // Act
        var (response, result) = await App.Client.GETAsync<
            SearchProcessedPostcodes.Endpoint,
            SearchProcessedPostcodes.Request,
            SearchProcessedPostcodes.Response
        >(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.ShouldNotBeNull();

        var postcodeResult = result.Postcodes.ShouldHaveSingleItem();

        // Ensure all 4 raw records are returned in the response list
        postcodeResult.EPCAssessments.Count.ShouldBe(4);

        // Aggregations must only evaluate the 2 records where IsLatest = true
        var agg = postcodeResult.EPCAggregation;
        agg.TotalAssessments.ShouldBe(2);
        agg.PercentageExpired.ShouldBe(0.00m);
        agg.PercentageExpiringInNextYear.ShouldBe(50.00m); // 1 out of 2 latest assessments

        // Rating distribution across the 2 latest records (A=50%, C=50%, historical G=0%)
        agg.EPCRatingPercentages["A"].ShouldBe(50.00m);
        agg.EPCRatingPercentages["C"].ShouldBe(50.00m);
        agg.EPCRatingPercentages["G"].ShouldBe(0.00m);
    }

    // ==========================================
    // 3. Search & Filter Logic
    // ==========================================

    [Fact]
    public async Task SearchProcessedPostcodes_NoSearchTermProvided_ReturnsAllProcessedPostcodes()
    {
        // Arrange
        const string lsoaCode = "E01014421";
        var now = SystemClock.Instance.GetCurrentInstant();

        var lsoaDeprivation = new LSOADeprivationBuilder { LSOACode = lsoaCode }.Build();
        Db.LSOADeprivation.Add(lsoaDeprivation);

        var postcode1 = new PostcodeBuilder
        {
            PostcodeKey = "BS1 1AA",
            LSOACode = lsoaCode,
            EPCsLastUpdatedAt = now,
        }.Build();

        var postcode2 = new PostcodeBuilder
        {
            PostcodeKey = "BS2 2BB",
            LSOACode = lsoaCode,
            EPCsLastUpdatedAt = now,
        }.Build();

        Db.Postcodes.Add(postcode1);
        Db.Postcodes.Add(postcode2);
        await Db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var request = new SearchProcessedPostcodes.Request(PostcodeSearchTerm: null);

        // Act
        var (response, result) = await App.Client.GETAsync<
            SearchProcessedPostcodes.Endpoint,
            SearchProcessedPostcodes.Request,
            SearchProcessedPostcodes.Response
        >(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.ShouldNotBeNull();

        result.Postcodes.Count.ShouldBe(2);
        result.Postcodes.ShouldContain(p => p.Postcode == "BS1 1AA");
        result.Postcodes.ShouldContain(p => p.Postcode == "BS2 2BB");
    }

    [Fact]
    public async Task SearchProcessedPostcodes_SearchTermProvided_FiltersByPostcodePrefixCaseInsensitively()
    {
        // Arrange
        const string lsoaCode = "E01014421";
        var now = SystemClock.Instance.GetCurrentInstant();

        var lsoaDeprivation = new LSOADeprivationBuilder { LSOACode = lsoaCode }.Build();
        Db.LSOADeprivation.Add(lsoaDeprivation);

        var targetPostcode = new PostcodeBuilder
        {
            PostcodeKey = "BS1 5AH",
            LSOACode = lsoaCode,
            EPCsLastUpdatedAt = now,
        }.Build();

        var nonMatchingPostcode = new PostcodeBuilder
        {
            PostcodeKey = "BA1 2DD",
            LSOACode = lsoaCode,
            EPCsLastUpdatedAt = now,
        }.Build();

        Db.Postcodes.Add(targetPostcode);
        Db.Postcodes.Add(nonMatchingPostcode);
        await Db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var request = new SearchProcessedPostcodes.Request(PostcodeSearchTerm: "bs1");

        // Act
        var (response, result) = await App.Client.GETAsync<
            SearchProcessedPostcodes.Endpoint,
            SearchProcessedPostcodes.Request,
            SearchProcessedPostcodes.Response
        >(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.ShouldNotBeNull();

        var postcodeResult = result.Postcodes.ShouldHaveSingleItem();
        postcodeResult.Postcode.ShouldBe("BS1 5AH");
    }

    [Fact]
    public async Task SearchProcessedPostcodes_PostcodeMissingLSOACodeOrEPCsLastUpdatedAt_ExcludesFromResults()
    {
        // Arrange
        const string validLsoaCode = "E01014421";
        var now = SystemClock.Instance.GetCurrentInstant();

        var lsoaDeprivation = new LSOADeprivationBuilder { LSOACode = validLsoaCode }.Build();
        Db.LSOADeprivation.Add(lsoaDeprivation);

        // 1. Fully valid processed postcode (Included)
        var validPostcode = new PostcodeBuilder
        {
            PostcodeKey = "BS1 9AA",
            LSOACode = validLsoaCode,
            EPCsLastUpdatedAt = now,
        }.Build();

        // 2. Missing LSOACode (Excluded)
        var missingLsoaPostcode = new PostcodeBuilder
        {
            PostcodeKey = "BS1 9BB",
            LSOACode = null,
            EPCsLastUpdatedAt = now,
        }.Build();

        // 3. Missing EPCsLastUpdatedAt timestamp (Excluded)
        var missingEpcTimestampPostcode = new PostcodeBuilder
        {
            PostcodeKey = "BS1 9CC",
            LSOACode = validLsoaCode,
            EPCsLastUpdatedAt = null,
        }.Build();

        // 4. Missing both mandatory fields (Excluded)
        var incompletePostcode = new PostcodeBuilder
        {
            PostcodeKey = "BS1 9DD",
            LSOACode = null,
            EPCsLastUpdatedAt = null,
        }.Build();

        Db.Postcodes.Add(validPostcode);
        Db.Postcodes.Add(missingLsoaPostcode);
        Db.Postcodes.Add(missingEpcTimestampPostcode);
        Db.Postcodes.Add(incompletePostcode);
        await Db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var request = new SearchProcessedPostcodes.Request(PostcodeSearchTerm: "BS1");

        // Act
        var (response, result) = await App.Client.GETAsync<
            SearchProcessedPostcodes.Endpoint,
            SearchProcessedPostcodes.Request,
            SearchProcessedPostcodes.Response
        >(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.ShouldNotBeNull();

        var postcodeResult = result.Postcodes.ShouldHaveSingleItem();
        postcodeResult.Postcode.ShouldBe("BS1 9AA");
    }
}
