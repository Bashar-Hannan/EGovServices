namespace EGovServices.Domain.Entities;

/// <summary>
/// يمثّل السجل الطبي الأساسي لمواطن في قاعدة البيانات الحكومية المحاكاة.
/// كل سجل مرتبط بمواطن واحد عبر NationalNumber — نفس نمط CriminalRecord تماماً.
/// المواطن لا يُدخل هذه البيانات بنفسه؛ هي بيانات حكومية موجودة مسبقاً
/// يجلبها النظام تلقائياً عند إصدار "الملف الطبي الإلكتروني".
/// </summary>
public class MedicalRecord
{
    /// <summary>Unique identifier for the record</summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// NationalNumber of the citizen this record belongs to.
    /// FK → Citizens.NationalNumber
    /// </summary>
    public required string CitizenNationalNumber { get; set; }

    /// <summary>فصيلة الدم — مثال: "A+", "O-"</summary>
    public required string BloodType { get; set; }

    /// <summary>الطول بالسنتيمتر</summary>
    public required int HeightCm { get; set; }

    /// <summary>الوزن بالكيلوغرام</summary>
    public required int WeightKg { get; set; }

    /// <summary>الحساسيات — "لا يوجد" إذا لا توجد</summary>
    public required string Allergies { get; set; }

    /// <summary>الأمراض المزمنة — "لا يوجد" إذا لا توجد</summary>
    public required string ChronicDiseases { get; set; }

    /// <summary>تاريخ آخر تحديث للسجل الطبي</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Citizen Citizen { get; set; } = null!;
}
