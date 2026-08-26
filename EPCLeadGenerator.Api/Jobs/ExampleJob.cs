using TickerQ.Utilities.Base;
using TickerQ.Utilities.Interfaces;

namespace EPCLeadGenerator.Api.Jobs;

public record ExampleJobPayload(string UserId);

public class ExampleJob : ITickerFunction<ExampleJobPayload>
{
    public async Task ExecuteAsync(
        TickerFunctionContext<ExampleJobPayload> context,
        CancellationToken cancellationToken
    )
    {
        Console.WriteLine($"Job {context.Id} executed, for user {context.Request.UserId}");

        // await exampleService.ExampleFunction(cancellationToken);
    }
}
