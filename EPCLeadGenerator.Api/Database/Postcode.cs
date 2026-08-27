using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NodaTime;

namespace EPCLeadGenerator.Api.Database;

public class Postcode
{
    [Key]
    [Column("Postcode")]
    [MaxLength(10)]
    public string PostcodeKey { get; set; } = null!;

    [MaxLength(20)]
    public string? LSOACode { get; set; }

    public bool MarkAsDone { get; set; }

    public Instant? EPCsLastUpdatedAt { get; set; }

    [ForeignKey(nameof(LSOACode))]
    public LSOADeprivation? LSOADeprivation { get; set; }
    public ICollection<EPCAssessment> EPCAssessments { get; set; } = new List<EPCAssessment>();
}
