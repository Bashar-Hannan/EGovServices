namespace EGovServices.Domain.Entities;

/// <summary>
/// مستشفى حكومي — جدول مستقل عن Branches (قرار معماري: فصل بيانات
/// المستشفيات لأنها تحتاج حقولاً خاصة بها: الاختصاص، الأسرّة، حالة
/// التشغيل والبنية التحتية — وهذه الحقول لا معنى لها لفروع الوزارات
/// الأخرى، وخلطها في Branches كان يمنع فلترة/توسعة الخدمة لاحقاً).
/// مرتبط بوزارة الصحة عبر GovernmentEntityId تماماً كـ Branch.
/// </summary>
public class Hospital
{
    public required Guid Id { get; set; }

    // ── الربط بوزارة الصحة ──────────────────────────────────────────
    public required Guid GovernmentEntityId { get; set; }

    // ── نفس حقول ملف الإكسل (توزع مشافي وزارة الصحة حسب المحافظات) ──
    public required string Name { get; set; }
    public required string Governorate { get; set; }
    public string? Address { get; set; }
    public string? Specialty { get; set; }              // الاختصاص
    public string? InfrastructureStatus { get; set; }    // البنية التحتية
    public string? OperationalStatus { get; set; }       // الحالة التشغيلية
    public int? BedsActual { get; set; }                 // الأسرّة الفعلي
    public int? BedsTheoretical { get; set; }             // الأسرّة النظري
    public string? PhoneNumber { get; set; }              // هاتف المشفى
    public string? AdminPhoneNumber { get; set; }         // هاتف المدير الاداري
    public string? DirectorPhoneNumber { get; set; }      // هاتف المدير

    // ── الإحداثيات لعرضها على خرائط جوجل — نفس شكل Branch (decimal(10,7)) ──
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }

    public required bool IsActive { get; set; }

    // Navigation
    public GovernmentEntity GovernmentEntity { get; set; } = null!;
}
