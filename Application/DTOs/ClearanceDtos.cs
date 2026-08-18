namespace EGovServices.Application.DTOs;

/// <summary>
/// بيانات توليد شهادة عدم المحكومية — نسخة موسّعة تطابق تصميم HTML الجديد.
/// كل الحقول الجديدة تُشتق من Citizen entity مباشرة (لا تحتاج مصدر بيانات إضافي).
/// </summary>
public sealed record ClearanceCertificatePdfData
{
    // ── بيانات الهوية الأساسية ──────────────────────────────────────
    public required string FirstName { get; init; }
    public required string FatherName { get; init; }
    public required string LastName { get; init; }   // النسبة
    public string? MotherName { get; init; }
    public required string FullName { get; init; }   // للتوافق مع الاستخدامات القديمة (رسائل، إلخ)
    public required string NationalNumber { get; init; }

    // ── بيانات إضافية من Citizen ─────────────────────────────────────
    public string? Religion { get; init; }
    public string? Gender { get; init; }
    public DateOnly? BirthDate { get; init; }
    public string? PlaceOfBirth { get; init; }
    public string? RecordPlace { get; init; }
    public string? RecordNumber { get; init; }
    public string? Address { get; init; }       // محل الإقامة الحالي

    // ── نتيجة الفحص الجنائي ───────────────────────────────────────────
    public required bool HasActiveCrimes { get; init; }
    public required string CheckResult { get; init; }
    public List<ClearanceCriminalRecordItem> CriminalRecords { get; init; } = [];

    // ── بيانات الوثيقة ─────────────────────────────────────────────────
    public required DateOnly IssueDate { get; init; }
    public required string ReferenceNumber { get; init; }
    public string? FormDataJson { get; init; }
    public required string VerificationToken { get; init; }
}

/// <summary>سجل واحد ضمن جدول الأحكام القضائية بالوثيقة.</summary>
public sealed record ClearanceCriminalRecordItem
{
    public required string CrimeDescription { get; init; }
    public required DateOnly JudgmentDate { get; init; }
    public required bool IsActive { get; init; }
}
