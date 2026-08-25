using JackTemplate.Api.Database;

namespace JackTemplate.Api.Services;

public interface IExampleService
{
    Task ExampleFunction(CancellationToken cancellationToken);
}

public class ExampleService(DataContext dataContext) : IExampleService
{
    public async Task ExampleFunction(CancellationToken cancellationToken)
    {
        await Task.Delay(500, cancellationToken);
    }
}
