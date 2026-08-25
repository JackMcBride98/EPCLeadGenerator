using JackTemplate.Api.Configuration;
using JackTemplate.Api.Database;
using JackTemplate.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace JackTemplate.Api.Features.User;

public class GetUser
{
    public record UserResponse(string UserId, string Name, DateTimeOffset CreatedAt);

    public record Response(UserResponse User);

    public class Endpoint(
        DataContext dataContext,
        IExampleService exampleService,
        IOptions<ExampleOptions> options
    ) : EndpointWithoutRequest<Response>
    {
        public override void Configure()
        {
            Get("/profile");
            AllowAnonymous();
            Description(b => b.ProducesProblemFE<ProblemDetails>(404));
        }

        public override async Task<Response> ExecuteAsync(CancellationToken ct)
        {
            var example = options.Value.Example;
            Console.WriteLine("This is an example option: " + example);

            var user = await dataContext.Users.FirstOrDefaultAsync(ct);

            if (user is null)
            {
                ThrowError("User not found", 404);
            }

            await exampleService.ExampleFunction(ct);

            return new Response(
                new UserResponse(user.UserId, user.Name, user.CreatedAt.ToDateTimeOffset())
            );
        }
    }
}
