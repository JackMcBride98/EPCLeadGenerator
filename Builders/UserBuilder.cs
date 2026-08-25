using Bogus;
using JackTemplate.Api.Database;
using NodaTime;
using NodaTime.Extensions;

namespace Builders;

public class UserBuilder : Builder<User>
{
    private static readonly Faker Faker = new();

    public string UserId { get; set; } = Faker.Random.Guid().ToString();
    public string Name { get; set; } = Faker.Internet.UserName();
    public Instant? CreatedAt { get; set; }

    public override User Build()
    {
        return new User(UserId, Name) { CreatedAt = CreatedAt ?? DateTime.UtcNow.ToInstant() };
    }
}
