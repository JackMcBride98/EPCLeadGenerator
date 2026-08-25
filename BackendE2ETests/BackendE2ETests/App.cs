using JackTemplate.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;

namespace Tests;

public class App : AppFixture<Program>
{
    public IExampleService MockExampleService { get; private set; } =
        Substitute.For<IExampleService>();

    public string DatabaseConnectionString =>
        Services.GetRequiredService<IConfiguration>().GetRequiredSection("Database")[
            "ConnectionString"
        ]
        ?? throw new InvalidOperationException(
            "Database connection string is missing in configuration"
        );

    public App()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
    }

    protected override async ValueTask PreSetupAsync() { }

    protected override void ConfigureApp(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        services.RemoveAll<IExampleService>();
        services.AddSingleton(MockExampleService);
    }
}
