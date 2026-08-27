using Bogus;
using EPCLeadGenerator.Api.Services;

namespace Builders.EPCApi;

public class EPCCertificateBuilder : Builder<EPCCertificate>
{
    private static readonly Faker Faker = new();

    public string CertificateNumber { get; set; } =
        $"CERT-{Faker.Random.AlphaNumeric(8).ToUpper()}";
    public string? AddressLine1 { get; set; } = Faker.Address.StreetAddress();
    public string? Postcode { get; set; } = Faker.Address.ZipCode();
    public string? CurrentEnergyEfficiencyBand { get; set; } =
        Faker.PickRandom("A", "B", "C", "D", "E", "F", "G");
    public DateTime RegistrationDate { get; set; } =
        Faker.Date.Recent(30).AddYears(Faker.Random.Int(-15, -5));
    public long? Uprn { get; set; } = Faker.Random.Long(100000000, 999999999);

    public override EPCCertificate Build() =>
        new(
            CertificateNumber: CertificateNumber,
            AddressLine1: AddressLine1,
            AddressLine2: null,
            AddressLine3: null,
            AddressLine4: null,
            Postcode: Postcode,
            PostTown: Faker.Address.City(),
            Council: null,
            Constituency: null,
            CurrentEnergyEfficiencyBand: CurrentEnergyEfficiencyBand,
            RegistrationDate: RegistrationDate.ToString("yyyy-MM-dd"),
            Uprn: Uprn,
            SchemaType: "RdSAP"
        );
}
