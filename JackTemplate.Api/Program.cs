using System.Reflection;
using System.Text.Json.Serialization;
using FastEndpoints.OpenApi;
using JackTemplate.Api.Configuration;
using JackTemplate.Api.Database;
using JackTemplate.Api.Jobs;
using JackTemplate.Api.Services;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using TickerQ.DependencyInjection;

var builder = WebApplication.CreateBuilder();

var dbSection = builder.Configuration.GetRequiredSection("Database");
var connectionString = dbSection["ConnectionString"];

var isDocumentGeneration =
    Assembly
        .GetEntryAssembly()
        ?.GetName()
        .Name?.Contains("GetDocument", StringComparison.OrdinalIgnoreCase)
    ?? false;

if (!isDocumentGeneration && string.IsNullOrWhiteSpace(connectionString))
{
    throw new Exception("Database connection string is missing");
}

builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddDbContextPool<DataContext>(options =>
{
    options.UseNpgsql(
        connectionString,
        x =>
        {
            x.UseNodaTime();
        }
    );
});
builder
    .Services.AddFastEndpoints()
    .OpenApiDocument(options =>
    {
        options.DocumentName = "v1";
        options.Title = "GetDocument.Insider API";
        options.Version = "v1.0.0";
        options.ShortSchemaNames = true;
    });

var optionsBuilder = builder
    .Services.AddOptions<ExampleOptions>()
    .Bind(builder.Configuration.GetSection(ExampleOptions.Position))
    .ValidateDataAnnotations();

if (!isDocumentGeneration)
{
    optionsBuilder.ValidateOnStart();
}

builder.Services.AddScoped<IExampleService, ExampleService>();
builder.Services.AddTickerQ();
builder.Services.MapTicker<ExampleJob, ExampleJobPayload>();

builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapHealthChecks("/api/health");

app.UseRouting();
app.UseDefaultExceptionHandler();
app.UseFastEndpoints(c =>
{
    c.Endpoints.RoutePrefix = "api";

    c.Serializer.Options.Converters.Add(new JsonStringEnumConverter());

    c.Endpoints.NameGenerator = ctx =>
    {
        var declaringType = ctx.EndpointType.DeclaringType;

        var name = declaringType != null ? declaringType.Name : ctx.EndpointType.Name;

        return name.EndsWith("Endpoint") ? name[..^8] : name;
    };

    c.Endpoints.Configurator = ep =>
    {
        ep.Description(d => d.ProducesProblemDetails(500));
    };

    c.Errors.UseProblemDetails(x =>
    {
        x.AllowDuplicateErrors = true;
        x.IndicateErrorCode = true;
        x.IndicateErrorSeverity = true;
        x.TypeValue = "https://www.rfc-editor.org/rfc/rfc7231#section-6.5.1";
        x.TitleValue = "One or more validation errors occurred.";
        x.TitleTransformer = pd =>
            pd.Status switch
            {
                400 => "Validation Error",
                404 => "Not Found",
                _ => "One or more errors occurred!",
            };
    });
});

// `UseEndpoints` terminates the request pipeline if a match was found. It's usually added implicitly by .NET but we
// need to add it explicitly because otherwise it would wrap everything, including the logic below to proxy to the
// Vite dev server in development. If we don't put it here, every request will fall through to the proxying logic,
// so in particular API calls etc. will not get handled correctly.
// See https://docs.microsoft.com/en-us/aspnet/core/fundamentals/routing?view=aspnetcore-6.0
app.UseEndpoints(_ => { });

app.UseTickerQ();

if (builder.Environment.IsDevelopment())
{
    app.UseSpa(spa => spa.UseProxyToSpaDevelopmentServer("http://localhost:3000"));
    app.MapOpenApi();
    app.MapScalarApiReference(o =>
    {
        o.AddDocuments("v1");
        o.OperationTitleSource = OperationTitleSource.Path;
    });
}
else
{
    if (!isDocumentGeneration && !builder.Environment.IsEnvironment("Testing"))
    {
        app.MapStaticAssets();
    }
    app.MapFallbackToFile("index.html");
}

app.Run();
