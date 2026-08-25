using JackTemplate.Api.Services;

namespace Tests.Services;

public class ExampleServiceTests(App app) : TestBase(app)
{
    [Fact]
    public async Task ExampleFunction_ShouldCompleteWithoutException()
    {
        // Arrange
        var exampleService = App.Services.GetRequiredService<IExampleService>();
        var cancellationToken = CancellationToken.None;

        // Act
        var exception = await Record.ExceptionAsync(() =>
            exampleService.ExampleFunction(cancellationToken)
        );

        // Assert
        Assert.Null(exception);
    }
}
