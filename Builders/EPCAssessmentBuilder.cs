using Bogus;
using Builders.Postcodes;
using EPCLeadGenerator.Api.Database;
using NodaTime;

namespace Builders;

public class EPCAssessmentBuilder : Builder<EPCAssessment>
{
    private static readonly Faker Faker = new();

    public int EPCAssessmentId { get; set; } = 0;
    public string PostcodeKey { get; set; } = Faker.Address.ZipCode().ToUpper();
    public string AddressLine { get; set; } = Faker.Address.StreetAddress();
    public string? EPCRating { get; set; } = Faker.PickRandom("A", "B", "C", "D", "E", "F", "G");
    public Instant RegistrationDate { get; set; } =
        SystemClock.Instance.GetCurrentInstant().Minus(Duration.FromDays(365 * 5));
    public Instant UpdatedAt { get; set; } = SystemClock.Instance.GetCurrentInstant();
    public Postcode? Postcode { get; set; } = null;

    public override EPCAssessment Build()
    {
        return new EPCAssessment
        {
            EPCAssessmentId = EPCAssessmentId,
            PostcodeKey = PostcodeKey,
            AddressLine = AddressLine,
            EPCRating = EPCRating,
            RegistrationDate = RegistrationDate,
            UpdatedAt = UpdatedAt,
            Postcode =
                Postcode
                ?? new PostcodeBuilder { PostcodeKey = PostcodeKey }
                    .WithLSOADeprivationData()
                    .Build(),
        };
    }
}
