using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NodaTime;

namespace EPCLeadGenerator.Api.Database;

public class EPCAssessment
{
    [Key]
    public int EPCAssessmentId { get; set; }

    [Column("Postcode")]
    [MaxLength(10)]
    public string PostcodeKey { get; set; } = null!;

    [MaxLength(1000)]
    public string AddressLine { get; set; } = null!;

    [MaxLength(2)]
    public string? EPCRating { get; set; }

    public bool IsExpired { get; set; }

    public Instant CreatedAt { get; set; }

    [ForeignKey(nameof(PostcodeKey))]
    public Postcode Postcode { get; set; } = null!;
}
