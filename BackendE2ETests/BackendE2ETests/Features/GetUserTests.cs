using Builders;
using JackTemplate.Api.Features.User;
using NodaTime;
using NSubstitute;

namespace Tests.Features;

public class GetProfileEndpointTests(App app) : TestBase(app)
{
    private const string DefaultUserId = "user_123";
    private const string DefaultName = "Jane Doe";

    [Fact]
    public async Task GetProfile_UserExists_ReturnsProfile()
    {
        // Arrange
        var createdAtInstant = Instant.FromUtc(2026, 7, 29, 10, 0, 0);

        var user = new UserBuilder
        {
            UserId = DefaultUserId,
            Name = DefaultName,
            CreatedAt = createdAtInstant,
        }.Build();

        Db.Users.Add(user);
        await Db.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var (response, result) = await App.Client.GETAsync<GetUser.Endpoint, GetUser.Response>();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        result.User.UserId.ShouldBe(DefaultUserId);
        result.User.Name.ShouldBe(DefaultName);
        result.User.CreatedAt.ShouldBe(createdAtInstant.ToDateTimeOffset());

        await App.MockExampleService.Received(1).ExampleFunction(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetProfile_UserNotInDatabase_ReturnsNotFound()
    {
        // Act
        var (response, _) = await App.Client.GETAsync<GetUser.Endpoint, GetUser.Response>();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
