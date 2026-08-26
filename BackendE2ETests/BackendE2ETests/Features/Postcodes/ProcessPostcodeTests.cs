using Builders;
using Builders.EPCApi;
using Builders.Postcodes;
using EPCLeadGenerator.Api.Features.Postcodes;
using EPCLeadGenerator.Api.Services;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using NodaTime.Extensions;
using NSubstitute;

namespace Tests.Features.Postcodes;

public class ProcessPostcodeTests(App app) : TestBase(app)
{
    [Fact]
    public async Task ProcessPostcodeLSOALookup_PostcodeRecordDoesNotExist_SuccessfulLSOALookup_SavesPostcodeWithLSOACode()
    {
        // Arrange
        const string postcode = "BS1 1AA";
        const string lsoaCode = "E01014421";

        await SetupLSOALookupSuccess(postcode, lsoaCode);
        SetupEPCLookupSuccess(postcode);

        var request = new ProcessPostcode.Request(postcode);

        // Act
        await App.Client.POSTAsync<
            ProcessPostcode.Endpoint,
            ProcessPostcode.Request,
            ProcessPostcode.Response
        >(request);

        // Assert
        Db.ChangeTracker.Clear();
        var dbRecord = await Db
            .Postcodes.Include(p => p.LSOADeprivation)
            .Include(p => p.EPCAssessments)
            .FirstOrDefaultAsync(
                p => p.PostcodeKey == postcode,
                TestContext.Current.CancellationToken
            );

        Assert.NotNull(dbRecord);
        Assert.Equal(lsoaCode, dbRecord.LSOACode);
    }

    [Fact]
    public async Task ProcessPostcodeLSOALookup_PostcodeRecordDoesNotExist_FailedLSOALookup_Returns404AndDoesNotSavePostcodeRecord()
    {
        // Arrange
        const string postcode = "BS1 9XX";

        SetupLSOALookupFailure(postcode, statusCode: 404, errorMessage: "Postcode not found");

        var request = new ProcessPostcode.Request(postcode);

        // Act
        var (response, _) = await App.Client.POSTAsync<
            ProcessPostcode.Endpoint,
            ProcessPostcode.Request,
            ProcessPostcode.Response
        >(request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        Db.ChangeTracker.Clear();
        var dbRecord = await Db.Postcodes.FirstOrDefaultAsync(
            p => p.PostcodeKey == postcode,
            TestContext.Current.CancellationToken
        );

        Assert.Null(dbRecord);
    }

    [Fact]
    public async Task ProcessPostcodeLSOALookup_PostcodeRecordExistsWithLSOACodeSet_DoesNotCallLSOAService_AndPostcodeUnchangedInDatabase()
    {
        // Arrange
        const string postcode = "BS1 2BB";
        const string existingLsoa = "E01014421";

        var existingPostcode = new PostcodeBuilder { PostcodeKey = postcode }
            .WithLSOADeprivationData(existingLsoa)
            .Build();

        Db.Postcodes.Add(existingPostcode);
        await Db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var request = new ProcessPostcode.Request(postcode);

        // Act
        await App.Client.POSTAsync<
            ProcessPostcode.Endpoint,
            ProcessPostcode.Request,
            ProcessPostcode.Response
        >(request);

        // Assert
        await App
            .MockPostcodeLookupService.DidNotReceive()
            .GetLSOAAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());

        Db.ChangeTracker.Clear();
        var dbRecord = await Db
            .Postcodes.Include(p => p.LSOADeprivation)
            .Include(p => p.EPCAssessments)
            .FirstOrDefaultAsync(
                p => p.PostcodeKey == postcode,
                TestContext.Current.CancellationToken
            );

        Assert.NotNull(dbRecord);
        Assert.Equal(existingLsoa, dbRecord.LSOACode);
        Assert.NotNull(dbRecord.LSOADeprivation);
    }

    [Fact]
    public async Task ProcessPostcodeLSOALookup_PostcodeRecordExistsButLSOACodeNotSet_CallsLSOAService_AndUpdatesPostcodeInDatabase()
    {
        // Arrange
        const string postcode = "BS1 3CC";
        const string newLsoa = "E01014421";

        var existingPostcode = new PostcodeBuilder
        {
            PostcodeKey = postcode,
            LSOACode = null,
        }.Build();

        Db.Postcodes.Add(existingPostcode);
        await Db.SaveChangesAsync(TestContext.Current.CancellationToken);

        await SetupLSOALookupSuccess(postcode, newLsoa);

        var request = new ProcessPostcode.Request(postcode);

        // Act
        await App.Client.POSTAsync<
            ProcessPostcode.Endpoint,
            ProcessPostcode.Request,
            ProcessPostcode.Response
        >(request);

        // Assert
        await App
            .MockPostcodeLookupService.Received(1)
            .GetLSOAAsync(postcode, Arg.Any<CancellationToken>());

        Db.ChangeTracker.Clear();
        var dbRecord = await Db
            .Postcodes.Include(p => p.LSOADeprivation)
            .Include(p => p.EPCAssessments)
            .FirstOrDefaultAsync(
                p => p.PostcodeKey == postcode,
                TestContext.Current.CancellationToken
            );

        Assert.NotNull(dbRecord);
        Assert.Equal(newLsoa, dbRecord.LSOACode);
        Assert.NotNull(dbRecord.LSOADeprivation);
    }

    [Fact]
    public async Task ProcessPostcodeLSOALookup_PostcodeWithWhitespaceAndLowerCase_TrimsAndUppercasesPostcode()
    {
        // Arrange
        const string rawPostcode = "  bs1 4dd  ";
        const string cleanPostcode = "BS1 4DD";
        const string lsoaCode = "E01014421";

        await SetupLSOALookupSuccess(cleanPostcode, lsoaCode);
        SetupEPCLookupSuccess(cleanPostcode);

        var request = new ProcessPostcode.Request(rawPostcode);

        // Act
        var (response, result) = await App.Client.POSTAsync<
            ProcessPostcode.Endpoint,
            ProcessPostcode.Request,
            ProcessPostcode.Response
        >(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.Equal(cleanPostcode, result.Postcode);

        await App
            .MockPostcodeLookupService.Received(1)
            .GetLSOAAsync(cleanPostcode, Arg.Any<CancellationToken>());

        Db.ChangeTracker.Clear();
        var dbRecord = await Db
            .Postcodes.Include(p => p.LSOADeprivation)
            .Include(p => p.EPCAssessments)
            .FirstOrDefaultAsync(
                p => p.PostcodeKey == cleanPostcode,
                TestContext.Current.CancellationToken
            );

        Assert.NotNull(dbRecord);
        Assert.Equal(cleanPostcode, dbRecord.PostcodeKey);
    }

    [Fact]
    public async Task ProcessPostcodeEPCCertificateLookup_PostcodeRecordDoesNotExist_SuccessfulLSOALookup_CallsEPCServiceAndSavesExpectedEPCAssessments()
    {
        // Arrange
        const string postcode = "BS1 5EE";
        const string lsoaCode = "E01014421";
        const string expectedCertNo = "CERT-ABC-123";
        const string expectedAddress = "10 Test Street, Bristol";
        const string expectedRating = "B";
        DateTime expectedRegistrationDate = DateTime.UtcNow.AddDays(-10).Date;

        await SetupLSOALookupSuccess(postcode, lsoaCode);

        var customCertificate = new EPCCertificateBuilder
        {
            CertificateNumber = expectedCertNo,
            Postcode = postcode,
            AddressLine1 = expectedAddress,
            CurrentEnergyEfficiencyBand = expectedRating,
            RegistrationDate = expectedRegistrationDate,
        }.Build();

        SetupEPCLookupSuccess(postcode, new List<EPCCertificate> { customCertificate });

        var request = new ProcessPostcode.Request(postcode);

        // Act
        var (response, result) = await App.Client.POSTAsync<
            ProcessPostcode.Endpoint,
            ProcessPostcode.Request,
            ProcessPostcode.Response
        >(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.Equal(postcode, result.Postcode);
        Assert.Equal("Postcode processed successfully.", result.Message);

        await App
            .MockEPCApiService.Received(1)
            .SearchCertificatesByPostcodeAsync(postcode, Arg.Any<CancellationToken>());

        Db.ChangeTracker.Clear();
        var dbRecord = await Db
            .Postcodes.Include(p => p.EPCAssessments)
            .FirstOrDefaultAsync(
                p => p.PostcodeKey == postcode,
                TestContext.Current.CancellationToken
            );

        Assert.NotNull(dbRecord);
        var assessment = Assert.Single(dbRecord.EPCAssessments);
        Assert.Equal(expectedAddress, assessment.AddressLine);
        Assert.Equal(expectedRating, assessment.EPCRating);
        Assert.Equal(postcode, assessment.PostcodeKey);
        Assert.Equal(expectedRegistrationDate.ToInstant(), assessment.RegistrationDate);
    }

    [Fact]
    public async Task ProcessPostcodeEPCCertificateLookup_PostcodeRecordDoesNotExist_SuccessfulLSOALookup_EPCServiceErrors_ThrowsError_EPCAssessmentsUnsaved_LSOASaved()
    {
        // Arrange
        const string postcode = "BS1 6FF";
        const string lsoaCode = "E01014421";

        await SetupLSOALookupSuccess(postcode, lsoaCode);
        SetupEPCLookupFailure(postcode, statusCode: 502, errorMessage: "EPC Gateway Error");

        var request = new ProcessPostcode.Request(postcode);

        // Act
        var (response, _) = await App.Client.POSTAsync<
            ProcessPostcode.Endpoint,
            ProcessPostcode.Request,
            ProcessPostcode.Response
        >(request);

        // Assert
        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync(
            TestContext.Current.CancellationToken
        );
        content.ShouldContain("EPC Gateway Error");
        Assert.Contains("EPC Gateway Error", content);

        Db.ChangeTracker.Clear();
        var dbRecord = await Db
            .Postcodes.Include(p => p.EPCAssessments)
            .FirstOrDefaultAsync(
                p => p.PostcodeKey == postcode,
                TestContext.Current.CancellationToken
            );

        Assert.NotNull(dbRecord);
        Assert.Equal(lsoaCode, dbRecord.LSOACode);
        Assert.Empty(dbRecord.EPCAssessments);
    }

    [Fact]
    public async Task ProcessPostcodeEPCCertificateLookup_CertificatesNull_Throws404Error()
    {
        // Arrange
        const string postcode = "BS1 7GG";
        const string lsoaCode = "E01014421";

        await SetupLSOALookupSuccess(postcode, lsoaCode);

        var result = new EPCSearchResultBuilder
        {
            IsSuccess = true,
            Certificates = null,
            ErrorMessage = null,
            StatusCode = 200,
        }.Build();

        App.MockEPCApiService.SearchCertificatesByPostcodeAsync(
                postcode,
                Arg.Any<CancellationToken>()
            )
            .Returns(result);

        var request = new ProcessPostcode.Request(postcode);

        // Act
        var (response, _) = await App.Client.POSTAsync<
            ProcessPostcode.Endpoint,
            ProcessPostcode.Request,
            ProcessPostcode.Response
        >(request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync(
            TestContext.Current.CancellationToken
        );
        content.ShouldContain($"EPC Certificate search returned null for Postcode {postcode}.");
    }

    [Fact]
    public async Task ProcessPostcodeEPCCertificateLookup_PostcodeRecordExists_ExistingEPCAssessments_DoesNotCallEPCService_AndEPCAssessmentsUnchangedInDatabase()
    {
        // Arrange
        const string postcode = "BS1 8HH";
        const string lsoaCode = "E01014421";
        Instant expectedUpdatedAt = SystemClock
            .Instance.GetCurrentInstant()
            .Minus(Duration.FromDays(5));

        var existingAssessment = new EPCAssessmentBuilder
        {
            PostcodeKey = postcode,
            AddressLine = "Old Address",
            EPCRating = "C",
            UpdatedAt = expectedUpdatedAt,
        }.Build();

        var existingPostcode = new PostcodeBuilder { PostcodeKey = postcode, LSOACode = lsoaCode }
            .WithLSOADeprivationData(lsoaCode)
            .Build();

        existingPostcode.EPCAssessments.Add(existingAssessment);

        Db.Postcodes.Add(existingPostcode);
        await Db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var request = new ProcessPostcode.Request(postcode, RefreshEPCData: false);

        // Act
        var response = await App.Client.POSTAsync<
            ProcessPostcode.Endpoint,
            ProcessPostcode.Request
        >(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        await App
            .MockEPCApiService.DidNotReceive()
            .SearchCertificatesByPostcodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());

        Db.ChangeTracker.Clear();
        var dbRecord = await Db
            .Postcodes.Include(p => p.EPCAssessments)
            .FirstOrDefaultAsync(
                p => p.PostcodeKey == postcode,
                TestContext.Current.CancellationToken
            );

        dbRecord.ShouldNotBeNull();
        var assessment = dbRecord.EPCAssessments.ShouldHaveSingleItem();
        assessment.AddressLine.ShouldBe("Old Address");
        assessment.EPCRating.ShouldBe("C");

        // Compare DateTime equivalents with a small tolerance (or compare truncated to milliseconds)
        assessment
            .UpdatedAt.ToDateTimeUtc()
            .ShouldBe(expectedUpdatedAt.ToDateTimeUtc(), TimeSpan.FromMilliseconds(10));
    }

    [Fact]
    public async Task ProcessPostcodeEPCCertificateLookup_PostcodeRecordExists_ExistingEPCAssessments_RefreshEPCDataTrue_CallsEPCServiceAndUpdatesEPCAssessments()
    {
        // Arrange
        const string postcode = "BS1 9II";
        const string lsoaCode = "E01014421";

        var oldRegistrationDate = DateTime.UtcNow.AddDays(-60).Date;
        var newRegistrationDate = DateTime.UtcNow.AddDays(-5).Date;

        var oldAssessment = new EPCAssessmentBuilder
        {
            PostcodeKey = postcode,
            AddressLine = "Old Address",
            EPCRating = "D",
            RegistrationDate = oldRegistrationDate.ToInstant(),
        }.Build();

        var existingPostcode = new PostcodeBuilder { PostcodeKey = postcode, LSOACode = lsoaCode }
            .WithLSOADeprivationData(lsoaCode)
            .Build();

        existingPostcode.EPCAssessments.Add(oldAssessment);

        Db.Postcodes.Add(existingPostcode);
        await Db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var newCertificate = new EPCCertificateBuilder
        {
            CertificateNumber = "CERT-NEW999",
            Postcode = postcode,
            CurrentEnergyEfficiencyBand = "A",
            AddressLine1 = "Old Address",
            RegistrationDate = newRegistrationDate,
        }.Build();

        SetupEPCLookupSuccess(postcode, new List<EPCCertificate> { newCertificate });

        var request = new ProcessPostcode.Request(postcode, RefreshEPCData: true);

        // Act
        var response = await App.Client.POSTAsync<
            ProcessPostcode.Endpoint,
            ProcessPostcode.Request
        >(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        await App
            .MockEPCApiService.Received(1)
            .SearchCertificatesByPostcodeAsync(postcode, Arg.Any<CancellationToken>());

        Db.ChangeTracker.Clear();
        var dbRecord = await Db
            .Postcodes.Include(p => p.EPCAssessments)
            .FirstOrDefaultAsync(
                p => p.PostcodeKey == postcode,
                TestContext.Current.CancellationToken
            );

        dbRecord.ShouldNotBeNull();
        var assessment = dbRecord.EPCAssessments.ShouldHaveSingleItem();
        assessment.EPCRating.ShouldBe("A");
        assessment.AddressLine.ShouldBe("Old Address");
        assessment.RegistrationDate.ShouldBe(newRegistrationDate.ToInstant());
        assessment.RegistrationDate.ShouldNotBe(oldRegistrationDate.ToInstant());
    }

    [Fact]
    public async Task ProcessPostcodeEPCCertificateLookup_PostcodeRecordExists_ExistingEPCAssessments_RefreshEPCDataTrue_EPCServiceThrowsError_ReturnsError_EPCAssessmentsUnsaved()
    {
        // Arrange
        const string postcode = "BS1 0JJ";
        const string lsoaCode = "E01014421";

        var expectedRegistrationDate = DateTime.UtcNow.AddDays(-20).Date;
        var expectedUpdatedAt = SystemClock
            .Instance.GetCurrentInstant()
            .Minus(Duration.FromDays(10));

        var originalAssessment = new EPCAssessmentBuilder
        {
            PostcodeKey = postcode,
            AddressLine = "Original Address",
            EPCRating = "B",
            RegistrationDate = expectedRegistrationDate.ToInstant(),
            UpdatedAt = expectedUpdatedAt,
        }.Build();

        var existingPostcode = new PostcodeBuilder { PostcodeKey = postcode, LSOACode = lsoaCode }
            .WithLSOADeprivationData(lsoaCode)
            .Build();

        existingPostcode.EPCAssessments.Add(originalAssessment);

        Db.Postcodes.Add(existingPostcode);
        await Db.SaveChangesAsync(TestContext.Current.CancellationToken);

        SetupEPCLookupFailure(
            postcode,
            statusCode: 502,
            errorMessage: "EPC API Failed during refresh"
        );

        var request = new ProcessPostcode.Request(postcode, RefreshEPCData: true);

        // Act
        var response = await App.Client.POSTAsync<
            ProcessPostcode.Endpoint,
            ProcessPostcode.Request
        >(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadGateway);

        Db.ChangeTracker.Clear();
        var dbRecord = await Db
            .Postcodes.Include(p => p.EPCAssessments)
            .FirstOrDefaultAsync(
                p => p.PostcodeKey == postcode,
                TestContext.Current.CancellationToken
            );

        dbRecord.ShouldNotBeNull();
        var assessment = dbRecord.EPCAssessments.ShouldHaveSingleItem();
        assessment.AddressLine.ShouldBe("Original Address");
        assessment.EPCRating.ShouldBe("B");
        assessment.RegistrationDate.ShouldBe(expectedRegistrationDate.ToInstant());
        assessment
            .UpdatedAt.ToDateTimeUtc()
            .ShouldBe(expectedUpdatedAt.ToDateTimeUtc(), TimeSpan.FromMilliseconds(10));
    }

    private async Task SetupLSOALookupSuccess(string postcode, string lsoaCode)
    {
        var lsoaDeprivation = new LSOADeprivationBuilder { LSOACode = lsoaCode }.Build();

        Db.LSOADeprivation.Add(lsoaDeprivation);
        await Db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = new PostcodeLookupResultBuilder
        {
            IsSuccess = true,
            LSOACode = lsoaCode,
            ErrorMessage = null,
            StatusCode = 200,
        }.Build();

        App.MockPostcodeLookupService.GetLSOAAsync(postcode, Arg.Any<CancellationToken>())
            .Returns(result);
    }

    private void SetupLSOALookupFailure(
        string postcode,
        int statusCode = 404,
        string errorMessage = "Not found"
    )
    {
        var result = new PostcodeLookupResultBuilder
        {
            IsSuccess = false,
            LSOACode = null,
            ErrorMessage = errorMessage,
            StatusCode = statusCode,
        }.Build();

        App.MockPostcodeLookupService.GetLSOAAsync(postcode, Arg.Any<CancellationToken>())
            .Returns(result);
    }

    private void SetupEPCLookupSuccess(string postcode, List<EPCCertificate>? certificates = null)
    {
        var certificateList =
            certificates
            ?? new List<EPCCertificate>
            {
                new EPCCertificateBuilder { Postcode = postcode }.Build(),
            };

        var result = new EPCSearchResultBuilder
        {
            IsSuccess = true,
            Certificates = certificateList,
            ErrorMessage = null,
            StatusCode = 200,
        }.Build();

        App.MockEPCApiService.SearchCertificatesByPostcodeAsync(
                postcode,
                Arg.Any<CancellationToken>()
            )
            .Returns(result);
    }

    private void SetupEPCLookupFailure(
        string postcode,
        int statusCode = 502,
        string errorMessage = "EPC error"
    )
    {
        var result = new EPCSearchResultBuilder
        {
            IsSuccess = false,
            Certificates = null,
            ErrorMessage = errorMessage,
            StatusCode = statusCode,
        }.Build();

        App.MockEPCApiService.SearchCertificatesByPostcodeAsync(
                postcode,
                Arg.Any<CancellationToken>()
            )
            .Returns(result);
    }
}
