using Builders;
using EPCLeadGenerator.Api.Features.Postcodes;

namespace Tests.Features.Postcodes;

public class GetPostcodeDeprivationEndpointTests(App app) : TestBase(app)
{
    [Fact]
    public async Task PostcodeLookup_ExistsWithLsoa_ReturnsDeprivationData()
    {
        // Arrange
        var lsoaBuilder = new LSOADeprivationBuilder
        {
            LSOACode = "E01012345",
            LSOAName = "Bristol 01A",
            MultipleDeprivationDecile = 4,
            MultipleDeprivationPercentage = 35.50m,
        };

        var postcode = new PostcodeBuilder { PostcodeKey = "BS8 1QU", MarkAsDone = true }
            .WithLSOADeprivationData(lsoaBuilder)
            .Build();

        if (postcode.LSOADeprivation is not null)
        {
            Db.LSOADeprivation.Add(postcode.LSOADeprivation);
        }

        Db.Postcodes.Add(postcode);
        await Db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var request = new GetPostcodeDeprivation.Request("BS8 1QU");

        // Act
        var (response, result) = await App.Client.POSTAsync<
            GetPostcodeDeprivation.Endpoint,
            GetPostcodeDeprivation.Request,
            GetPostcodeDeprivation.Response
        >(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        result.Data.ShouldNotBeNull();
        result.Data.Postcode.ShouldBe("BS8 1QU");
        result.Data.MarkAsDone.ShouldBeTrue();
        result.Data.LSOACode.ShouldBe("E01012345");
        result.Data.LSOAName.ShouldBe("Bristol 01A");
        result.Data.MultipleDeprivationPercentage.ShouldBe(35.50m);
        result.Data.MultipleDeprivationDecile.ShouldBe(4);
    }

    [Fact]
    public async Task PostcodeLookup_NotInDatabase_ReturnsNotFound()
    {
        // Arrange
        var request = new GetPostcodeDeprivation.Request("XX9 9XX");

        // Act
        var (response, _) = await App.Client.POSTAsync<
            GetPostcodeDeprivation.Endpoint,
            GetPostcodeDeprivation.Request,
            GetPostcodeDeprivation.Response
        >(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
