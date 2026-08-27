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
    public long UniquePropertyReferenceNumber { get; set; } =
        Faker.Random.Long(100000000000, 999999999999);
    public string CertificateNumber { get; set; } =
        Faker.Random.Replace("****-****-****-****-****");
    public bool IsLatest { get; set; } = true;
    public Instant RegistrationDate { get; set; } =
        SystemClock.Instance.GetCurrentInstant().Minus(Duration.FromDays(365 * 5));
    public Instant UpdatedAt { get; set; } = SystemClock.Instance.GetCurrentInstant();
    public Postcode? Postcode { get; set; } = null;

    public EPCAssessmentBuilder WithIsLatest(bool isLatest)
    {
        IsLatest = isLatest;
        return this;
    }

    public EPCAssessmentBuilder WithExpired()
    {
        RegistrationDate = SystemClock
            .Instance.GetCurrentInstant()
            .Minus(Duration.FromDays(365 * 11));
        return this;
    }

    public EPCAssessmentBuilder WithExpiringInNextYear()
    {
        RegistrationDate = SystemClock
            .Instance.GetCurrentInstant()
            .Minus(Duration.FromDays((int)(365 * 9.5)));
        return this;
    }

    public override EPCAssessment Build()
    {
        return new EPCAssessment
        {
            EPCAssessmentId = EPCAssessmentId,
            PostcodeKey = PostcodeKey,
            AddressLine = AddressLine,
            EPCRating = EPCRating,
            UniquePropertyReferenceNumber = UniquePropertyReferenceNumber,
            CertificateNumber = CertificateNumber,
            IsLatest = IsLatest,
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
