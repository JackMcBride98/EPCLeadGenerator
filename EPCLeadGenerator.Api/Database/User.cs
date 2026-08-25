using System.ComponentModel.DataAnnotations;
using NodaTime;
using NodaTime.Extensions;

namespace JackTemplate.Api.Database;

public class User(string userId, string name)
{
    [MaxLength(100)]
    public string UserId { get; set; } = userId;

    [MaxLength(5000)]
    public string Name { get; set; } = name;

    public Instant CreatedAt { get; set; } = DateTime.UtcNow.ToInstant();
}
