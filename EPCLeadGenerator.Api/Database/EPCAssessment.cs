using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NodaTime;

namespace EPCLeadGenerator.Api.Database;

public class EPCAssessment
{
    public const int EPCAssessmentExpiryDayCount = 365 * 10; // 10 years

    [Key]
    public int EPCAssessmentId { get; set; }

    [Column("Postcode")]
    [MaxLength(10)]
    public string PostcodeKey { get; set; } = null!;

    [MaxLength(1000)]
    public string AddressLine { get; set; } = null!;

    [MaxLength(2)]
    public string? EPCRating { get; set; }

    public Instant RegistrationDate { get; set; }

    public bool IsExpired =>
        RegistrationDate
        < SystemClock
            .Instance.GetCurrentInstant()
            .Minus(Duration.FromDays(EPCAssessmentExpiryDayCount));

    public Instant UpdatedAt { get; set; }

    [ForeignKey(nameof(PostcodeKey))]
    public Postcode Postcode { get; set; } = null!;
}
