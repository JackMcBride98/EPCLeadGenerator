using JackTemplate.Api.Services;
using TickerQ.Utilities.Base;
using TickerQ.Utilities.Interfaces;

namespace JackTemplate.Api.Jobs;

public record ExampleJobPayload(string UserId);

public class ExampleJob(IExampleService exampleService) : ITickerFunction<ExampleJobPayload>
{
    public async Task ExecuteAsync(
        TickerFunctionContext<ExampleJobPayload> context,
        CancellationToken cancellationToken
    )
    {
        Console.WriteLine($"Job {context.Id} executed, for user {context.Request.UserId}");

        await exampleService.ExampleFunction(cancellationToken);
    }
}
