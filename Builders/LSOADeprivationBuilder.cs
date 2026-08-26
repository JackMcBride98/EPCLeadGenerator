using Bogus;
using EPCLeadGenerator.Api.Database;

namespace Builders;

public class LSOADeprivationBuilder : Builder<LSOADeprivation>
{
    private static readonly Faker Faker = new();

    public string LSOACode { get; set; } = $"E01{Faker.Random.Number(100000, 999999)}";
    public string LSOAName { get; set; } =
        $"{Faker.Address.City()} {Faker.Random.AlphaNumeric(3).ToUpper()}";

    public int MultipleDeprivationRank { get; set; } = Faker.Random.Number(1, 33755);
    public int MultipleDeprivationDecile { get; set; } = Faker.Random.Number(1, 10);
    public decimal MultipleDeprivationPercentage { get; set; } =
        Math.Round(Faker.Random.Decimal(1.0m, 99.99m), 2);

    public int IncomeRank { get; set; } = Faker.Random.Number(1, 33755);
    public int IncomeDecile { get; set; } = Faker.Random.Number(1, 10);
    public decimal IncomePercentage { get; set; } =
        Math.Round(Faker.Random.Decimal(1.0m, 99.99m), 2);

    public int EmploymentRank { get; set; } = Faker.Random.Number(1, 33755);
    public int EmploymentDecile { get; set; } = Faker.Random.Number(1, 10);
    public decimal EmploymentPercentage { get; set; } =
        Math.Round(Faker.Random.Decimal(1.0m, 99.99m), 2);

    public int BarriersToHousingAndServicesRank { get; set; } = Faker.Random.Number(1, 33755);
    public int BarriersToHousingAndServicesDecile { get; set; } = Faker.Random.Number(1, 10);
    public decimal BarriersToHousingAndServicesPercentage { get; set; } =
        Math.Round(Faker.Random.Decimal(1.0m, 99.99m), 2);

    public override LSOADeprivation Build()
    {
        return new LSOADeprivation
        {
            LSOACode = LSOACode,
            LSOAName = LSOAName,
            MultipleDeprivationRank = MultipleDeprivationRank,
            MultipleDeprivationDecile = MultipleDeprivationDecile,
            MultipleDeprivationPercentage = MultipleDeprivationPercentage,
            IncomeRank = IncomeRank,
            IncomeDecile = IncomeDecile,
            IncomePercentage = IncomePercentage,
            EmploymentRank = EmploymentRank,
            EmploymentDecile = EmploymentDecile,
            EmploymentPercentage = EmploymentPercentage,
            BarriersToHousingAndServicesRank = BarriersToHousingAndServicesRank,
            BarriersToHousingAndServicesDecile = BarriersToHousingAndServicesDecile,
            BarriersToHousingAndServicesPercentage = BarriersToHousingAndServicesPercentage,
        };
    }
}
