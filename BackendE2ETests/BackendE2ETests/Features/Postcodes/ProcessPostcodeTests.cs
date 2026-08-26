using Builders;
using Builders.EPCApi;
using Builders.Postcodes;
using EPCLeadGenerator.Api.Features.Postcodes;
using EPCLeadGenerator.Api.Services;
using Microsoft.EntityFrameworkCore;
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

    private async Task SetupLSOALookupSuccess(string postcode, string lsoaCode)
    {
        var lsoaDeprivation = new LSOADeprivationBuilder { LSOACode = lsoaCode }.Build();

        Db.LSOADeprivation.Add(lsoaDeprivation);
        await Db.SaveChangesAsync();

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
}
