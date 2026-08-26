using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace EPCLeadGenerator.Api.Database;

public class LSOADeprivation
{
    [Key]
    [MaxLength(20)]
    public string LSOACode { get; set; } = null!;

    [MaxLength(150)]
    public string LSOAName { get; set; } = null!;

    public int MultipleDeprivationRank { get; set; }
    public int MultipleDeprivationDecile { get; set; }

    [Precision(5, 2)]
    public decimal MultipleDeprivationPercentage { get; set; }

    public int IncomeRank { get; set; }
    public int IncomeDecile { get; set; }

    [Precision(5, 2)]
    public decimal IncomePercentage { get; set; }

    public int EmploymentRank { get; set; }
    public int EmploymentDecile { get; set; }

    [Precision(5, 2)]
    public decimal EmploymentPercentage { get; set; }

    public int BarriersToHousingAndServicesRank { get; set; }
    public int BarriersToHousingAndServicesDecile { get; set; }

    [Precision(5, 2)]
    public decimal BarriersToHousingAndServicesPercentage { get; set; }

    public ICollection<Postcode> Postcodes { get; set; } = new List<Postcode>();
}
