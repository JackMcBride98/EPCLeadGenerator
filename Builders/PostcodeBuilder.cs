using Bogus;
using EPCLeadGenerator.Api.Database;

namespace Builders;

public class PostcodeBuilder : Builder<Postcode>
{
    private static readonly Faker Faker = new();

    public string PostcodeKey { get; set; } = $"{Faker.Address.ZipCode()}";
    public string? LSOACode { get; set; }
    public bool MarkAsDone { get; set; } = false;
    public LSOADeprivation? LSOADeprivation { get; set; } = null;

    public PostcodeBuilder WithLSOADeprivationData(LSOADeprivationBuilder? lsoaBuilder = null)
    {
        var lsoa = (lsoaBuilder ?? new LSOADeprivationBuilder()).Build();
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
        };
    }
}
