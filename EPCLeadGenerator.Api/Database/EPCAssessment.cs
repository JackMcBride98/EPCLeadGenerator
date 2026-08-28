using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using NodaTime;

namespace EPCLeadGenerator.Api.Database;

public class EPCAssessment
{
    public const int EPCAssessmentExpiryDayCount = 365 * 10; // 10 years
    public const int EPCAssessmentExpiringSoonDayCount = 365 * 9; // 9 years

    [Key]
    public long EPCAssessmentId { get; set; }

    [Column("UniquePropertyReferenceNumber")]
    public long UniquePropertyReferenceNumber { get; set; }

    [Column("Postcode")]
    [MaxLength(10)]
    public string PostcodeKey { get; set; } = null!;

    [MaxLength(1000)]
    public string AddressLine { get; set; } = null!;

    [MaxLength(2)]
    public string? EPCRating { get; set; }

    public Instant RegistrationDate { get; set; }

    public Instant UpdatedAt { get; set; }

    public bool IsLatest { get; set; }

    [MaxLength(24)]
    public string CertificateNumber { get; set; } = null!;

    [ForeignKey(nameof(PostcodeKey))]
    public Postcode Postcode { get; set; } = null!;

    [NotMapped]
    public bool IsExpired =>
        IsExpiredExpression.Compile()(this, SystemClock.Instance.GetCurrentInstant());

    [NotMapped]
    public bool IsExpiringInNextYear =>
        IsExpiringInNextYearExpression.Compile()(this, SystemClock.Instance.GetCurrentInstant());

    public static Expression<Func<EPCAssessment, Instant, bool>> IsExpiredExpression =>
        (a, now) => a.RegistrationDate < now.Minus(Duration.FromDays(EPCAssessmentExpiryDayCount));

    public static Expression<Func<EPCAssessment, Instant, bool>> IsExpiringInNextYearExpression =>
        (a, now) =>
            a.RegistrationDate >= now.Minus(Duration.FromDays(EPCAssessmentExpiryDayCount))
            && a.RegistrationDate < now.Minus(Duration.FromDays(EPCAssessmentExpiringSoonDayCount));
}
