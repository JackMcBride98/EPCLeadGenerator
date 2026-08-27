using Bogus;
using EPCLeadGenerator.Api.Database;
using NodaTime;

namespace Builders.Postcodes;

public class PostcodeBuilder : Builder<Postcode>
{
    private static readonly Faker Faker = new();

    public string PostcodeKey { get; set; } = $"{Faker.Address.ZipCode()}";
    public string? LSOACode { get; set; }
    public bool MarkAsDone { get; set; } = false;
    public Instant? EPCsLastUpdatedAt { get; set; }
    public LSOADeprivation? LSOADeprivation { get; set; } = null;

    public PostcodeBuilder WithLSOADeprivationData(LSOADeprivationBuilder? lsoaBuilder = null)
    {
        var lsoa = (lsoaBuilder ?? new LSOADeprivationBuilder()).Build();
        LSOADeprivation = lsoa;
        LSOACode = lsoa.LSOACode;
        return this;
    }

    public PostcodeBuilder WithLSOADeprivationData(string lsoaCode)
    {
        var lsoa = new LSOADeprivationBuilder { LSOACode = lsoaCode }.Build();
        LSOADeprivation = lsoa;
        LSOACode = lsoa.LSOACode;
        return this;
    }

    public override Postcode Build()
    {
        return new Postcode
        {
            PostcodeKey = PostcodeKey,
            LSOACode = LSOACode ?? LSOADeprivation?.LSOACode,
            MarkAsDone = MarkAsDone,
            LSOADeprivation = LSOADeprivation,
            EPCsLastUpdatedAt = EPCsLastUpdatedAt,
        };
    }
}
