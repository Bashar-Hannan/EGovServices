using System.ComponentModel.DataAnnotations.Schema;

namespace EGovServices.Domain.Entities;

/// <summary>
/// مخالفة مرورية — محدَّثة بحقول إضافية لمعرفة مصدر المخالفة.
/// </summary>
public class TrafficViolation
{
    public required Guid Id { get; set; }
    public required string ViolationNumber { get; set; }   // "VIO-2026-000001"
    public required string PlateNumber { get; set; }   // رقم اللوحة
    public required string CitizenNationalNumber { get; set; }   // FK → Citizens
    public required string ViolationType { get; set; }   // نوع المخالفة
    public string? Description { get; set; }   // وصف إضافي
    public required decimal Amount { get; set; }   // المبلغ
    public required string Location { get; set; }   // موقع المخالفة
    public required DateTime ViolationDate { get; set; }   // تاريخ المخالفة
    public required string Status { get; set; }   // "Open" | "Paid"
    public DateTime? PaidAt { get; set; }
    public Guid? WalletTransactionId { get; set; }

    // ── NEW — حقول مصدر المخالفة ─────────────────────────────────────
    /// <summary>اسم الضابط الذي أصدر المخالفة — null إذا كانت آلية</summary>
    public string? IssuedByOfficerName { get; set; }

    /// <summary>نوع المركبة — "سيارة" | "شاحنة" | "دراجة نارية" | "باص"</summary>
    public string? VehicleType { get; set; }

    /// <summary>رقم الكاميرا إذا كانت المخالفة مرصودة آلياً — null إذا كانت يدوية</summary>
    public string? CameraId { get; set; }

    // Navigation
    [ForeignKey(nameof(CitizenNationalNumber))]
    public Citizen Citizen { get; set; } = null!;
    public WalletTransaction? WalletTransaction { get; set; }
}
